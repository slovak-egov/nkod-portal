﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using VDS.RDF.Storage;

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

        public async Task<UserRecord?> GetOrCreateUser(string id, string? firstName, string? lastName, string? email, string? publisher, string? invitation)
        {
            UserRecord? user = await Users.FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName) && !string.IsNullOrWhiteSpace(invitation))
                {
                    user = await Users.FirstOrDefaultAsync(u => u.FirstName == firstName && u.LastName == lastName && !u.IsActive && u.InvitationToken != null && u.InvitationToken == invitation && u.InvitedAt.HasValue && DateTimeOffset.UtcNow <= u.InvitedAt.Value.AddHours(48));
                    if (user is not null)
                    {                        
                        Users.Remove(user);
                        await SaveChangesAsync();

                        user.Id = id;
                        user.ActivatedAt = DateTimeOffset.UtcNow;
                        user.InvitationToken = null;
                        user.IsActive = true;
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
                        Role = "PublisherAdmin",
                        IsActive = true
                    };

                    await Users.AddAsync(user);
                    await SaveChangesAsync();
                }
            }
            else if (!user.IsActive)
            {
                user = null;
            }

            return user;
        }

        public async Task<UserRecord?> GetUserByInvitation(string inviation)
        {
            return await Users.FirstOrDefaultAsync(u => u.InvitationToken == inviation);
        }
    }
}
