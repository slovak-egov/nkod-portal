using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IAM
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<UserRecord> Users => Set<UserRecord>();

        public async Task<UserRecord?> FindUser(string id, string publisherId)
        {
            return await Users.FirstOrDefaultAsync(u => u.Id == id && u.Publisher == publisherId);
        }
    }
}
