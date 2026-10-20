using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class AdminNotificationInput
    {
        public string? Subject { get; set; }

        public string? Body { get; set; }

        public ValidationResults Validate()
        {
            ValidationResults results = new ValidationResults();

            results.ValidateRequiredText(nameof(Subject), Subject);
            results.ValidateRequiredText(nameof(Body), Body);

            return results;
        }
    }
}
