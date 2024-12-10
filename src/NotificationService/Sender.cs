using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using MimeKit;
using System.Text;
using static System.Collections.Specialized.BitVector32;

namespace NotificationService
{
    public class Sender : ISender
    {
        private readonly EmailOptions options;

        public Sender(EmailOptions options)
        {
            this.options = options;
        }

        public async Task Send(string email, string body)
        {
            using SmtpClient client = new SmtpClient();
            await client.ConnectAsync(options.Host, options.Port ?? 25, options.UseSsl);
            await client.AuthenticateAsync(options.Username, options.Password);

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(options.FromName, options.FromAddress));
            message.To.Add(new MailboxAddress(string.Empty, email));
            message.Subject = "";
            message.Body = new TextPart("html") { Text = body };

            await client.SendAsync(message);
        }
    }
}
