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
        public Task SendEmail(string toEmail, string subject, string body)
        {
            return Task.CompletedTask;
        }
    }
}
