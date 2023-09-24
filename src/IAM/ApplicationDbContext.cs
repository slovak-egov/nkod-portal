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

        public async Task<UserRecord?> GetOrCreateUser(string id, string? firstName, string? lastName, string? email, string? publisher)
        {
            UserRecord? user = await Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                if (!string.IsNullOrEmpty(publisher) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
                {
                    user = new UserRecord
                    {
                        Id = id,
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Publisher = publisher,
                        Role = "PublisherAdmin"
                    };

                    await Users.AddAsync(user);
                    await SaveChangesAsync();
                }
            }

            return user;
        }
    }
}
