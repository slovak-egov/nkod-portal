using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Text;

namespace NotificationService
{
    public class SenderAccumulator
    {
        private readonly SenderAccumulatorLock senderAccumulatorLock;

        private readonly ISender sender;

        private readonly MainDbContext context;

        public SenderAccumulator(ISender sender, MainDbContext context, SenderAccumulatorLock senderAccumulatorLock)
        {
            this.sender = sender;
            this.context = context;
            this.senderAccumulatorLock = senderAccumulatorLock; ;
        }

        public async Task<bool> TrySend(string email, IEnumerable<INotification> notifications)
        {
            if (await senderAccumulatorLock.Semaphore.WaitAsync(TimeSpan.Zero))
            {
                try
                {
                    using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

                    DateTimeOffset limit = DateTimeOffset.Now.AddHours(-1);
                    if (await context.SentEmails.AnyAsync(n => n.Email == email && n.Sent >= limit))
                    {
                        return false;
                    }

                    StringBuilder body = new StringBuilder();

                    await sender.Send(email, body.ToString());

                    SentEmail sentEmail = new SentEmail
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = email,
                        Sent = DateTimeOffset.Now,
                    };
                    context.Add(sentEmail);

                    await context.SaveChangesAsync();
                    await tx.CommitAsync();

                    return true;
                }
                finally
                {
                    senderAccumulatorLock.Semaphore.Release();
                }
            }
            else
            {
                return false;
            }
        }
    }
}
