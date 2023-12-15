using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.RegularExpressions;

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

        public async Task<UserRecord?> GetOrCreateUser(string id, string? firstName, string? lastName, string? email, string? publisher, string? identificationNumber)
        {
            UserRecord? user = await Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) && !string.IsNullOrEmpty(identificationNumber))
                {
                    Match match = Regex.Match(identificationNumber, "^rc:\\/\\/sk\\/([0-9]+).*$");
                    if (match.Success)
                    {
                        identificationNumber = match.Groups[1].Value;
                    }

                    user = await Users.FirstOrDefaultAsync(u => u.FirstName == firstName && u.LastName == lastName && u.IdentificationNumber == identificationNumber);
                    if (user is not null)
                    {                        
                        Users.Remove(user);
                        await SaveChangesAsync();

                        user.Id = id;
                        await Users.AddAsync(user);
                        await SaveChangesAsync();
                    }
                }
                
                if (user is null && !string.IsNullOrEmpty(publisher) && !string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
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
