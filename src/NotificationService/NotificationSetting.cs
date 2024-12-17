using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NotificationService
{
    [Index(nameof(AuthKey))]
    public class NotificationSetting
    {
        [Key]
        public string Email { get; set; } = string.Empty;

        public string AuthKey { get; set; } = string.Empty;

        public bool IsDisabled { get; set; }
    }
}
