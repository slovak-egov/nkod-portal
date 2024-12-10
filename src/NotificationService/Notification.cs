using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NotificationService
{
    [Index(nameof(Email))]
    [Index(nameof(Url))]
    [Index(nameof(Sent))]
    public class Notification : INotification
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        
        public bool IsDeleted { get; set; }

        public DateTimeOffset Created { get; set; }

        public DateTimeOffset? Sent { get; set; }
    }
}
