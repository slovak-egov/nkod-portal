using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class MonitoringNotificationSettings
    {
        public bool NotifyIfLocalCatalogIsUnavaiable { get; set; }

        public bool HarvestationFailed { get; set; }

        public bool SuccessRatioIsLow { get; set; }

        public bool ProblemsDetected { get; set; }

        public bool Always { get; set; }

        public string? EmailAddresses { get; set; }

        public string? UseForLocalCatalog { get; set; }

        public List<string>? IgnoreLocalCatalogs { get; set; }
    }
}
