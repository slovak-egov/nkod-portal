using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NotificationService
{
    [Index(nameof(Tag))]
    public class NotificationTag
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string NotificationId { get; set; } = string.Empty;

        public string Tag { get; set; } = string.Empty;
    }
}
