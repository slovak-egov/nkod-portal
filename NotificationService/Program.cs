using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NotificationService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MainDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("Connection string not found.");
    }

    options.UseMySQL(connectionString);
});

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    options.EnableAdaptiveSampling = false;
});

string? frontendUrl = builder.Configuration["FrontendUrl"];
EmailOptions? emailOptions = builder.Configuration.GetSection("EmailOptions").Get<EmailOptions>();
if (emailOptions is not null)
{
    builder.Services.AddSingleton(sp => new Sender(emailOptions, sp));
}

var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    using MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
    await context.Database.MigrateAsync();
}

app.MapPost("/notification", async ([FromBody] NotificationsInput notifications, [FromServices] MainDbContext context, [FromServices] Sender sender) =>
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
                Notification notification = new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = n.Email,
                    Url = n.Url,
                    Description = n.Description,
                    Created = created,
                    Sent = sent ? sentOn : null,
                };

                context.Add(notification);
            }
        }

        await context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    return Results.Ok();
});

app.MapDelete("/notification/url", async ([FromQuery] string? url, [FromServices] MainDbContext context) =>
{
    if (!string.IsNullOrEmpty(url))
    {
        using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();
        foreach (Notification notification in context.Notifications.Where(n => n.Url == url && !n.IsDeleted && n.Sent == null))
        {
            notification.IsDeleted = true;
        }
        await context.SaveChangesAsync();
        await tx.CommitAsync();

        return Results.Ok();
    }
    return Results.BadRequest();
});

app.Run();
