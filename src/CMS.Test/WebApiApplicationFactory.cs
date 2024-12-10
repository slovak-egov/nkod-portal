using J2N.Collections.Generic.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using Piranha;
using Piranha.Data.EF.SQLite;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace CMS.Test
{
    public class ApiApplicationFactory : WebApplicationFactory<Program>
    {
        private const string DefaultIssuer = "http://test/";

        private const string DefaultAudience = "http://test/";

        private readonly byte[] defaultKey;

        private readonly string connectionString;

        private readonly SqliteConnection defaultConnection;

        private readonly string path;

        public ApiApplicationFactory(byte[]? defaultKey = null)
        {
            this.defaultKey = defaultKey ?? RandomNumberGenerator.GetBytes(32);

            Type dbContextType = typeof(Db<SQLiteDb>);
            FieldInfo dbContextInitiazliedField = dbContextType.GetField("IsInitialized", BindingFlags.NonPublic | BindingFlags.Static)!;
            dbContextInitiazliedField.SetValue(null, false);

            connectionString = $"Data Source=\"{Guid.NewGuid():N}\";Mode=Memory;Cache=Shared";
            defaultConnection = new SqliteConnection(connectionString);

            path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            FileStorage = new Storage(path);
            TestNotificationService = new TestNotificationService();
        }

        public string CreateToken(string? role, string? publisher = null, string name = "Test User", int lifetimeMinutes = 15, string? companyName = null, string? userId = null, string? userEmail = null, byte[]? key = null, string? userFormattedName = null)
        {
            if ((role == "Publisher" || role == "PublisherAdmin") && publisher is null)
            {
                publisher = "https://example.com";
            }

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
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            if (!string.IsNullOrEmpty(userFormattedName))
            {
                claims.Add(new Claim(ClaimTypes.Name, userFormattedName));
            }
            claims.Add(new Claim(ClaimTypes.Name, name));

            key ??= defaultKey;

            JwtSecurityToken token = new JwtSecurityToken(
                DefaultIssuer,
                DefaultAudience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

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
                        IssuerSigningKey = new SymmetricSecurityKey(defaultKey),
                        ValidateIssuerSigningKey = true
                    };
                });

                services.RemoveAll(s => s.ServiceType == typeof(IDocumentStorageClient));
                services.RemoveAll(s => s.ServiceType == typeof(INotificationService));

                services.AddSingleton<IDocumentStorageClient>(sp => new TestDocumentStorageClient(FileStorage, AnonymousAccessPolicy.Default));
                services.AddSingleton<INotificationService>(TestNotificationService);
            });
        }

        protected void ConfigureHost(IHostBuilder builder)
        {
            defaultConnection.Open();

            builder.ConfigureHostConfiguration(context =>
            {
                context.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:piranha"] = connectionString
                });
            });

            //if (!QueryExecutionListener.Enabled)
            {
                Piranha.App.CacheLevel = Piranha.Cache.CacheLevel.None;
            }
        }
                
        protected override IHost CreateHost(IHostBuilder builder)
        {
            ConfigureHost(builder);

            return base.CreateHost(builder);
        }

        public IApi CreateApi()
        {
            IServiceScope scope = Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IApi>();
        }

        public IFileStorage FileStorage { get; }

        public TestNotificationService TestNotificationService { get; }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            defaultConnection?.Dispose();

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
