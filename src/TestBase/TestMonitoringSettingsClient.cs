using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBase
{
    public class TestMonitoringSettingsClient : IMonitoringSettingsClient
    {
        public Task<MonitoringNotificationSettings?> GetSettingsForLocalCatalog(string id)
        {
            return Task.FromResult<MonitoringNotificationSettings?>(new MonitoringNotificationSettings());
        }

        public Task<MonitoringNotificationSettings?> GetSettingsForUser()
        {
            return Task.FromResult<MonitoringNotificationSettings?>(new MonitoringNotificationSettings());
        }

        public Task SaveSettigs(MonitoringNotificationSettings? settings)
        {
            return Task.CompletedTask;
        }
    }
}
