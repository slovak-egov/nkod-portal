using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Frontend.Test
{
    class WebApiApplicationFactory : WebApplicationFactory<Program>, ITokenService
    {
        private readonly IFileStorage storage;

        private const string DefaultIssuer = "http://test/";

        private const string DefaultAudience = "http://test/";

        private readonly byte[] defaultKey = RandomNumberGenerator.GetBytes(32);

        private TestIdentityAccessManagementClient? testIdentityAccessManagementClient;

        private IHost? host;

        public WebApiApplicationFactory(IFileStorage storage)
        {
            this.storage = storage;
        }

        public TestIdentityAccessManagementClient TestIdentityAccessManagementClient => Services.GetRequiredService<TestIdentityAccessManagementClient>();

        public string CreateUserAndToken(string? role, string? publisher = null, string? companyName = null, bool createPublisherIfNeeded = true)
        {
            string id = Guid.NewGuid().ToString();

            if (!string.IsNullOrEmpty(publisher))
            {
                IFileStorageAccessPolicy accessPolicy = new AllAccessFilePolicy();
                FileState? publisherState = storage.GetPublisherState(publisher, accessPolicy);

                if (publisherState is null && createPublisherIfNeeded)
                {
                    FoafAgent agent = FoafAgent.Create(new Uri(publisher));
                    FileMetadata metadata = new FileMetadata(Guid.NewGuid(), "Test", FileType.PublisherRegistration, null, publisher, true, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
                    storage.InsertFile(agent.ToString(), metadata, true, accessPolicy);
                }              
            }

            Services.GetRequiredService<TestIdentityAccessManagementClient>().AddUser(publisher, new PersistentUserInfo
            {
                FirstName = "Test",
                LastName = "User",
                Role = role,
                Email = "test@example.com",
                Id = id
            });

            return CreateToken(id, role, publisher, companyName);
        }

        public string CreateToken(string id, string? role, string? publisher = null, string? companyName = null)
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
            claims.Add(new Claim(ClaimTypes.Name, "Test User"));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, id));

            JwtSecurityToken token = new JwtSecurityToken(
                DefaultIssuer,
                DefaultAudience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(defaultKey), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSolutionRelativeContentRoot(".");
            builder.UseWebRoot("frontend/build");

            builder.ConfigureServices(services =>
            {
                services.AddSingleton(storage);
                services.AddSingleton<IDocumentStorageClient, TestDocumentStorageClient>();
                services.AddTransient<IFileStorageAccessPolicy, DefaultFileAccessPolicy>();

                services.AddSingleton<ILanguagesSource, DefaultLanguagesSource>();
                services.AddSingleton<ICodelistProviderClient, InternalCodelistProvider>();
                services.AddTransient(s => testIdentityAccessManagementClient ??= new TestIdentityAccessManagementClient(s.GetRequiredService<IHttpContextValueAccessor>(), this));
                services.AddTransient<IIdentityAccessManagementClient>(s => s.GetRequiredService<TestIdentityAccessManagementClient>());

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
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            IHost testHost = builder.Build();

            builder.ConfigureWebHost(builder =>
            {
                builder.UseKestrel().UseUrls("http://localhost:6001");
            });

            host = builder.Build();

            host.Start();

            IServer server = host.Services.GetRequiredService<IServer>();
            IServerAddressesFeature? addresses = server.Features.Get<IServerAddressesFeature>();

            ClientOptions.BaseAddress = addresses!.Addresses
                .Select(x => new Uri(x))
                .Last();

            testHost.Start();
            return testHost;
        }

        public Task<TokenResult> RefreshToken(string token, string refreshToken)
        {
            return Task.FromResult(new TokenResult
            {
                Token = token,
                RefreshToken = refreshToken
            });
        }

        public Task<TokenResult> DelegateToken(IHttpContextValueAccessor httpContextValueAccessor, string publisherId)
        {
            if (httpContextValueAccessor.HasRole("Superadmin"))
            {
                return Task.FromResult(new TokenResult
                {
                    Token = CreateToken("Superadmin", publisherId),
                    RefreshToken = "12345"
                });
            }
            else
            {
                throw new HttpRequestException("Forbidden", null, System.Net.HttpStatusCode.Forbidden);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                host?.Dispose();
                host = null;
            }
        }
    }
}

