using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Test
{
    public class TestSender : ISender
    {
        private readonly IServiceProvider serviceProvider;

        public TestSender(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task Send(string email, string body)
        {
            if (!Sent.TryGetValue(email, out List<string>? sent))
            {
                sent = new List<string>();
                Sent.Add(email, sent);
            }
            sent.Add(body);
            return Task.CompletedTask;
        }

        public Dictionary<string, List<string>> Sent { get; } = [];
    }
}
