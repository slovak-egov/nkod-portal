using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Infrastructure.Internal;
using Google.Protobuf.WellKnownTypes;
using J2N.Collections.Generic.Extensions;
using NkodSk.Abstractions;
using TestBase;
using Microsoft.Extensions.Hosting;
using System.Security.Policy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Abstractions;

namespace IAM.Test
{
    public class WebApiApplicationFactory : WebApplicationFactory<Program>
    {
        private const string DefaultIssuer = "http://data.gov.sk/";

        private const string DefaultAudience = "http://data.gov.sk/";

        private readonly SigningCredentials signingCredentials;

        private readonly Action<IHostBuilder>? hostBuilder;

        public WebApiApplicationFactory(Action<IHostBuilder>? hostBuilder = null)
        {
            this.hostBuilder = hostBuilder;
            RSA rsa = RSA.Create(2048);
            RsaSecurityKey securityKey = new RsaSecurityKey(rsa);
            signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512);
        }

        public IHttpContextValueAccessor CreateAccessor(string? role = null, string? publisher = null, string? id = null)
        {
            string? token = null;
            if (!string.IsNullOrEmpty(role))
            {
                token = CreateAccessToken(role, publisher, id: id);
            }
            return new StaticHttpContextValueAccessor(publisher, token, role, id);
        }

        public string CreateAccessToken(string? role, string? publisher = null, string name = "Test User", int lifetimeMinutes = 15, string? companyName = null, string? id = null)
        {
            List<Claim> claims = new List<Claim>();
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            if (!string.IsNullOrEmpty(publisher))
            {
                claims.Add(new Claim("Publisher", publisher));
            }
            if (!string.IsNullOrEmpty(companyName))
            {
                claims.Add(new Claim("CompanyName", companyName));
            }
            if (!string.IsNullOrEmpty(id))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
            }
            claims.Add(new Claim(ClaimTypes.Name, name));

            JwtSecurityToken token = new JwtSecurityToken(
                DefaultIssuer,
                DefaultAudience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateToken(string? role, string? publisher = null, string name = "Test User", int lifetimeMinutes = 15, string? companyName = null, string? id = null)
        {
            List<Claim> claims = new List<Claim>();
            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
            if (!string.IsNullOrEmpty(publisher))
            {
                claims.Add(new Claim("Publisher", publisher));
            }
            if (!string.IsNullOrEmpty(companyName))
            {
                claims.Add(new Claim("CompanyName", companyName));
            }
            if (!string.IsNullOrEmpty(id))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
            }
            claims.Add(new Claim(ClaimTypes.Name, name));

            JwtSecurityToken token = new JwtSecurityToken(
                DefaultIssuer,
                DefaultAudience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: signingCredentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = DefaultIssuer,
                ValidAudience = DefaultAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingCredentials.Key,
                ValidateLifetime = true
            };

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);            
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(s => s.ServiceType == typeof(ApplicationDbContext));
                services.RemoveAll(s => s.ServiceType == typeof(DbContextOptions));
                services.RemoveAll(s => s.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                services.RemoveAll(s => s.ServiceType == typeof(SigningCredentials));
                services.RemoveAll(s => s.ServiceType == typeof(IEmailService));

                services.AddSingleton(signingCredentials);
                services.AddSingleton<TestEmailService>();
                services.AddSingleton<IEmailService, TestEmailService>(p => p.GetRequiredService<TestEmailService>());

                string name = Guid.NewGuid().ToString();

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(name);
                    options.ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
                });
            });

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.Audience = DefaultAudience;
                    o.Authority = DefaultIssuer;
                    o.RequireHttpsMetadata = false;

                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = DefaultIssuer,
                        ValidAudience = DefaultAudience,
                        IssuerSigningKey = signingCredentials.Key,
                        ValidateIssuerSigningKey = true
                    };
                });
            });

            hostBuilder?.Invoke(builder);

            return base.CreateHost(builder);
        }

        public async Task<UserRecord> CreateCommunityUser(string email, string password)
        {
            using IServiceScope scope = Services.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            UserRecord record = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                Role = "CommunityUser",
                IsActive = true,
                FirstName = "Test",
                LastName = "User",
            };

            record.SetPassword(password);

            context.Users.Add(record);
            await context.SaveChangesAsync();
            return record;
        }
    }
}
