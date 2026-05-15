using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public interface IMonitoringSettingsClient
    {
        Task<MonitoringNotificationSettings?> GetSettingsForUser();

        Task<MonitoringNotificationSettings?> GetSettingsForLocalCatalog(string id);

        Task SaveSettigs(MonitoringNotificationSettings? settings);
    }
}
