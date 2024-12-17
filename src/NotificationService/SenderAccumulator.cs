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

        private readonly string? frontendUrl;

        public SenderAccumulator(ISender sender, MainDbContext context, SenderAccumulatorLock senderAccumulatorLock, string? frontendUrl)
        {
            this.sender = sender;
            this.context = context;
            this.senderAccumulatorLock = senderAccumulatorLock; ;
            this.frontendUrl = frontendUrl;
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

                    NotificationSetting? setting = await context.GetOrCreateNotificationSettings(email);
                    if (setting is not null && setting.IsDisabled)
                    {
                        return true;
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


                    if (frontendUrl is not null)
                    {
                        Uri settingUrl = new Uri(new Uri(frontendUrl), "/sprava-notifikacii?auth=" + HttpUtility.UrlEncode(setting?.AuthKey ?? string.Empty));
                        body.AppendLine($"Tento e-mail bol odoslaný na adresu {HttpUtility.HtmlEncode(email)}. <a href=\"{HttpUtility.HtmlAttributeEncode(settingUrl.OriginalString)}\">Upraviť nastavenia odosielaných správ môžete na tejto stránke.</a>");
                        body.AppendLine("<br>");
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
