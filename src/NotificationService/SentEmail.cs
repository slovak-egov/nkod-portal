using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace NotificationService
{
    [Index(nameof(Email), nameof(Sent))]
    public class SentEmail
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTimeOffset Sent { get; set; }
    }
}
