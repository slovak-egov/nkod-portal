using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Text;
using System.Web;

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
                    DateTimeOffset limit = DateTimeOffset.Now.AddHours(-1);
                    if (await context.SentEmails.AnyAsync(n => n.Email == email && n.Sent >= limit))
                    {
                        return false;
                    }

                    StringBuilder body = new StringBuilder();

                    body.Append("Dobrý deň,<br>na stránke portálu Národného katalógu otvorených dát pribudol nový obsah.<br><br>");

                    foreach (INotification notification in notifications)
                    {
                        if (!string.IsNullOrEmpty(notification.Url))
                        {
                            body.Append($"<a href=\"{HttpUtility.HtmlAttributeEncode(notification.Url)}\">");
                        }

                        body.AppendLine(HttpUtility.HtmlEncode(notification.Title));

                        if (!string.IsNullOrEmpty(notification.Url))
                        {
                            body.AppendLine("</a>");
                        }

                        body.AppendLine("<br>");

                        if (!string.IsNullOrEmpty(notification.Description))
                        {
                            body.AppendLine(HttpUtility.HtmlEncode(notification.Description));
                            body.AppendLine("<br>");
                        }

                        body.AppendLine("<br>");
                    }

                    body.AppendLine("Tím centrálneho portálu otvorených dát data.slovensko.sk");

                    await sender.Send(email, body.ToString());

                    SentEmail sentEmail = new SentEmail
                    {
                        Id = Guid.NewGuid().ToString(),
                        Email = email,
                        Sent = DateTimeOffset.Now,
                    };
                    context.Add(sentEmail);

                    await context.SaveChangesAsync();

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
