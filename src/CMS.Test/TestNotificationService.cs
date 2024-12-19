using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMS.Test
{
    public class TestNotificationService : INotificationService
    {
        public void Delete(string tag)
        {
            Notifications.RemoveAll(n => n.Item5.Contains(tag));
        }

        public void Notify(string email, string url, string title, string description, List<string> tags)
        {
            Notifications.Add((email, url, title, description, tags.ToArray()));
        }

        public List<(string, string, string, string, string[])> Notifications { get; } = new List<(string, string, string, string, string[])>();
    }
}
