using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestSuperadminNotificationService : ISuperadminNotificationService
    {
        private readonly List<(string, string)> notifications = new List<(string, string)>();

        public Task Notify(string subject, string body)
        {
            notifications.Add((subject, body));
            return Task.CompletedTask;
        }

        public IReadOnlyList<(string, string)> Notifications => notifications;
    }
}
