using Microsoft.EntityFrameworkCore;

namespace NotificationService
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<NotificationTag> NotificationTags => Set<NotificationTag>();

        public DbSet<SentEmail> SentEmails => Set<SentEmail>();
    }
}
