
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static System.Collections.Specialized.BitVector32;

namespace NotificationService
{
    public class SenderService : IHostedService
    {
        private readonly Sender sender;

        private readonly Timer timer;

        private readonly IServiceProvider serviceProvider;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        public SenderService(Sender sender, IServiceProvider serviceProvider)
        {
            this.sender = sender;
            this.serviceProvider = serviceProvider;
            timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        private async void OnTimerTick(object? state)
        {
            if (await semaphore.WaitAsync(TimeSpan.Zero))
            {
                try
                {
                    MainDbContext context = serviceProvider.GetRequiredService<MainDbContext>();
                    using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

                    List<Notification> notifications = await context.Notifications.Where(n => !n.IsDeleted && n.Sent == null).ToListAsync();

                    if (notifications.Count > 0)
                    {
                        Dictionary<string, List<Notification>> notificationsByEmail = notifications.GroupByKey(n => n.Email);

                        foreach ((string email, List<Notification> emailNotifications) in notificationsByEmail)
                        {
                            if (await sender.TrySend(email, emailNotifications))
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

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
