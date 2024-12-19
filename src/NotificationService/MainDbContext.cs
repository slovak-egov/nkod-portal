using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace NotificationService
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions<MainDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<NotificationTag> NotificationTags => Set<NotificationTag>();

        public DbSet<SentEmail> SentEmails => Set<SentEmail>();

        public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();

        public async Task<NotificationSetting> GetOrCreateNotificationSettings(string email)
        {
            email = email.ToLowerInvariant();
            NotificationSetting? setting = await NotificationSettings.FirstOrDefaultAsync(e => e.Email == email);
            if (setting is null)
            {
                setting = NotificationSettings.Local.FirstOrDefault(e => e.Email == email);
            }

            if (setting is null)
            {
                byte[] buffer = new byte[16];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(buffer);
                }

                StringBuilder sb = new StringBuilder();
                foreach (byte b in buffer)
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                setting = new NotificationSetting { Email = email, AuthKey = sb.ToString() };
                Add(setting);
            }

            return setting;
        }
    }
}
