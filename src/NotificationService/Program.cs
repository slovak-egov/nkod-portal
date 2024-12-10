using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
builder.Services.AddScoped<SenderAccumulator>();
builder.Services.AddSingleton<SenderAccumulatorLock>();
builder.Services.AddSingleton<SenderService>();
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

        using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

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
        await tx.CommitAsync();
    }

    return Results.Ok();
});

app.MapDelete("/notification/tag", async ([FromQuery] string? tag, [FromServices] MainDbContext context) =>
{
    if (!string.IsNullOrEmpty(tag))
    {
        using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();
        foreach (NotificationTag notificationTag in context.NotificationTags.Where(n => n.Tag == tag))
        {
            Notification? notification = await context.Notifications.FindAsync(notificationTag.NotificationId);
            if (notification is not null && !notification.IsDeleted && !notification.Sent.HasValue)
            {
                notification.IsDeleted = true;
            }
        }
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Ok();
    }
    return Results.BadRequest();
});

app.Run();

public partial class Program { }