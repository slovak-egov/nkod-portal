using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MySql.EntityFrameworkCore.Extensions;
using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMySQLServer<MainDbContext>(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty);

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    options.EnableAdaptiveSampling = false;
});

string? frontendUrl = builder.Configuration["FrontendUrl"];
EmailOptions? emailOptions = builder.Configuration.GetSection("EmailOptions").Get<EmailOptions>();
if (emailOptions is not null)
{
    builder.Services.AddSingleton<ISender>(sp => new Sender(emailOptions));
}
builder.Services.AddSingleton<SenderAccumulatorLock>();
builder.Services.AddSingleton<SenderService>();
builder.Services.AddScoped(sp => new SenderAccumulator(sp.GetRequiredService<ISender>(), sp.GetRequiredService<MainDbContext>(), sp.GetRequiredService<SenderAccumulatorLock>(), frontendUrl));
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SenderService>());

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    using MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    if (context.Database.IsMySql())
    {
        await context.Database.MigrateAsync();
    }
}

app.MapPost("/notification", async ([FromBody] NotificationsInput notifications, [FromServices] MainDbContext context, [FromServices] SenderAccumulator sender) =>
{
    if (notifications.Notifications.Count > 0)
    {
        DateTimeOffset created = DateTimeOffset.Now;

        Dictionary<string, List<NotificationInput>> notificationsByEmail = notifications.Notifications.GroupByKey(n => n.Email);

        foreach ((string email, List<NotificationInput> emailNotifications) in notificationsByEmail)
        {
            bool sent = await sender.TrySend(email, emailNotifications);
            DateTimeOffset sentOn = DateTimeOffset.Now;
            foreach (NotificationInput n in emailNotifications)
            {
                string id = Guid.NewGuid().ToString();

                Notification notification = new Notification
                {
                    Id = id,
                    Email = n.Email,
                    Url = n.Url,
                    Title = n.Title,
                    Description = n.Description,
                    Created = created,
                    Sent = sent ? sentOn : null,
                };

                context.Add(notification);

                if (n.Tags is not null)
                {
                    foreach (string tag in n.Tags)
                    {
                        context.Add(new NotificationTag { Id = Guid.NewGuid().ToString(), NotificationId = id, Tag = tag });
                    }
                }
            }
        }

        await context.SaveChangesAsync();
    }

    return Results.Ok();
});

app.MapDelete("/notification/tag", async ([FromQuery] string? tag, [FromServices] MainDbContext context) =>
{
    if (!string.IsNullOrEmpty(tag))
    {
        foreach (NotificationTag notificationTag in context.NotificationTags.Where(n => n.Tag == tag))
        {
            Notification? notification = await context.Notifications.FindAsync(notificationTag.NotificationId);
            if (notification is not null && !notification.IsDeleted && !notification.Sent.HasValue)
            {
                notification.IsDeleted = true;
            }
        }
        await context.SaveChangesAsync();

        return Results.Ok();
    }
    return Results.BadRequest();
});

app.MapPost("/notification/get", async ([FromBody] NotificationSetting setting, [FromServices] MainDbContext context) =>
{
    NotificationSetting? current = null;
    if (!string.IsNullOrEmpty(setting.AuthKey))
    {
        current = context.NotificationSettings.FirstOrDefault(e => e.AuthKey == setting.AuthKey);
    }

    if (current is null && !string.IsNullOrEmpty(setting.Email))
    {
        current = await context.GetOrCreateNotificationSettings(setting.Email);
    }

    if (current is not null)
    {
        return Results.Ok(new NotificationSetting
        {
            Email = current.Email,
            IsDisabled = current.IsDisabled,
        });
    }

    return Results.BadRequest();
});

app.MapPost("/notification/set", async ([FromBody] NotificationSetting setting, [FromServices] MainDbContext context) =>
{
    NotificationSetting? updated = null;
    if (!string.IsNullOrEmpty(setting.AuthKey))
    {
        updated = context.NotificationSettings.FirstOrDefault(e => e.AuthKey == setting.AuthKey);
    }

    if (updated is null && !string.IsNullOrEmpty(setting.Email))
    {
        updated = await context.GetOrCreateNotificationSettings(setting.Email);
    }

    if (updated is not null)
    {
        updated.IsDisabled = setting.IsDisabled;
        await context.SaveChangesAsync();

        return Results.Ok();
    }

    return Results.BadRequest();
});

app.Run();

public partial class Program { }