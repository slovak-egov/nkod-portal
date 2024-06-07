using Abstractions;
using MailKit.Net.Smtp;
using MimeKit;
using MySqlX.XDevAPI;
using System.Net;

namespace IAM
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string host;

        private readonly int port;

        private readonly string username;

        private readonly string password;

        private readonly bool useSsl;
        
        private readonly string fromEmail;

        private readonly string? fromName;

        public SmtpEmailService(string? host, int port, string? username, string? password, bool useSsl, string? fromEmail, string? fromName)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Host is required", nameof(host));
            }
            if (string.IsNullOrEmpty(fromEmail))
            {
                throw new ArgumentException("From email is required", nameof(fromEmail));
            }
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("From email is required", nameof(username));
            }
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
            this.useSsl = useSsl;
            this.fromEmail = fromEmail;
            this.fromName = fromName;
        }

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            using SmtpClient client = new SmtpClient();
            await client.ConnectAsync(host, port, useSsl);
            await client.AuthenticateAsync(username, password);

            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            await client.SendAsync(message);
        }
    }
}
