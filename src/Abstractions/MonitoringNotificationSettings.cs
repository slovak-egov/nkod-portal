using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            foreach (string line in EmailAddresses?.Split("\n") ?? [])
            {
                string address = line?.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(address))
                {
                    results.ValidateEmail(nameof(EmailAddresses), address, true);
                }
            }

            return results;
        }

        public void CopyTo(MonitoringNotificationSettings other)
        {
            other.NotifyIfLocalCatalogIsUnavaiable = NotifyIfLocalCatalogIsUnavaiable;
            other.HarvestationFailed = HarvestationFailed;
            other.SuccessRatioIsLow = SuccessRatioIsLow;
            other.ProblemsDetected = ProblemsDetected;
            other.Always = Always;
            other.EmailAddresses = EmailAddresses;
            other.UseForLocalCatalog = UseForLocalCatalog;
            other.IgnoreLocalCatalogs = IgnoreLocalCatalogs?.ToList();
        }
    }
}
