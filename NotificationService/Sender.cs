using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using MimeKit;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace NotificationService
{
    public class Sender
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        private readonly EmailOptions options;

        private readonly IServiceProvider serviceProvider;

        public Sender(EmailOptions options, IServiceProvider serviceProvider)
        {
            this.options = options;
            this.serviceProvider = serviceProvider;
        }

        public async Task<bool> TrySend(string email, IEnumerable<INotification> notifications)
        {
            if (await semaphore.WaitAsync(TimeSpan.Zero))
            {
                try
                {
                    MainDbContext context = serviceProvider.GetRequiredService<MainDbContext>();
                    using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();

                    DateTimeOffset limit = DateTimeOffset.Now.AddHours(-1);
                    if (await context.SentEmails.AnyAsync(n => n.Email == email && n.Sent >= limit))
                    {
                        return false;
                    }

                    StringBuilder body = new StringBuilder();

                    using SmtpClient client = new SmtpClient();
                    await client.ConnectAsync(options.Host, options.Port ?? 25, options.UseSsl);
                    await client.AuthenticateAsync(options.Username, options.Password);

                    MimeMessage message = new MimeMessage();
                    message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
                    message.To.Add(new MailboxAddress(string.Empty, email));
                    message.Subject = "";
                    message.Body = new TextPart("html") { Text = body.ToString() };

                    await client.SendAsync(message);

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
                    semaphore.Release();
                }
            }
            else
            {
                return false;
            }
        }
    }
}
