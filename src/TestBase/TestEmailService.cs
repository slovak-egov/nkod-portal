using Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestEmailService : IEmailService
    {
        private List<(string, string, string)> emails = new List<(string, string, string)>();

        public Task SendEmail(string toEmail, string subject, string body)
        {
            emails.Add((toEmail, subject, body));
            return Task.CompletedTask;
        }

        public IReadOnlyList<(string, string, string)> Emails => emails;
    }
}
