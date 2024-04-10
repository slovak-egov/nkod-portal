using Abstractions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace IAM
{
    public class EmailService : IEmailService
    {
        private readonly SendGridClient client;

        private readonly string? fromEmail;

        private readonly string? fromName;

        public EmailService(string? sendGridKey, string? fromEmail, string? fromName)
        {
            client = new SendGridClient(sendGridKey);
            this.fromEmail = fromEmail;
            this.fromName = fromName;
        }

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            SendGridMessage message = new SendGridMessage
            {
                From = new EmailAddress(fromEmail, fromName),
                Subject = subject,
                HtmlContent = body
            };
            message.AddTo(new EmailAddress(toEmail));
            await client.SendEmailAsync(message);
        }
    }
}
