using Abstractions;
using MySqlX.XDevAPI;
using SendGrid.Helpers.Mail;
using SendGrid;
using System.Net.Mail;
using System.Net;

namespace IAM
{
    public class SmtpEmailService : IEmailService
    {
        private readonly SmtpClient client;

        private readonly string fromEmail;

        private readonly string? fromName;

        public SmtpEmailService(string? host, int port, string username, string password, bool useSsl, string? fromEmail, string? fromName)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Host is required", nameof(host));
            }
            if (string.IsNullOrEmpty(fromEmail))
            {
                throw new ArgumentException("From email is required", nameof(fromEmail));
            }

            client = new SmtpClient(host, port);
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }
            client.EnableSsl = useSsl;
            this.fromEmail = fromEmail;
            this.fromName = fromName;
        }

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            MailMessage message = new MailMessage(new MailAddress(fromEmail, fromName), new MailAddress(toEmail))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            await client.SendMailAsync(message);
        }
    }
}
