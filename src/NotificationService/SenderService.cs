
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static System.Collections.Specialized.BitVector32;

namespace NotificationService
{
    public class SenderService : IHostedService
    {
        private Timer? timer;

        private readonly IServiceProvider serviceProvider;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public SenderService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task Run()
        {
            if (await semaphore.WaitAsync(TimeSpan.Zero))
            {
                try
                {
                    using IServiceScope scope = serviceProvider.CreateScope();

                    SenderAccumulator senderAccumulator = scope.ServiceProvider.GetRequiredService<SenderAccumulator>();

                    using MainDbContext context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
                    using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

                    List<Notification> notifications = await context.Notifications.Where(n => !n.IsDeleted && n.Sent == null).ToListAsync();

                    if (notifications.Count > 0)
                    {
                        Dictionary<string, List<Notification>> notificationsByEmail = notifications.GroupByKey(n => n.Email);

                        foreach ((string email, List<Notification> emailNotifications) in notificationsByEmail)
                        {
                            if (await senderAccumulator.TrySend(email, emailNotifications))
                            {
                                foreach (Notification notification in emailNotifications)
                                {
                                    notification.Sent = DateTimeOffset.Now;
                                }
                            }
                        }

                        await context.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private async void OnTimerTick(object? state)
        {
            await Run();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Dispose();
            return Task.CompletedTask;
        }
    }
}
