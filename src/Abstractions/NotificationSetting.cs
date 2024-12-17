using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NkodSk.Abstractions
{
    public class NotificationSetting
    {
        public string Email { get; set; } = string.Empty;

        public bool IsDisabled {  get; set; }
    }
}
