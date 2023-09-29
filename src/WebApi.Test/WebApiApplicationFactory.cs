using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace WebApi.Test
{
    public class WebApiApplicationFactory : WebApplicationFactory<Program>, ITokenService
    {
        private readonly IFileStorage storage;

        private const string DefaultIssuer = "http://test/";

        private const string DefaultAudience = "http://test/";

        private readonly byte[] defaultKey = RandomNumberGenerator.GetBytes(32);

        private TestIdentityAccessManagementClient? testIdentityAccessManagementClient;

        public WebApiApplicationFactory(IFileStorage storage)
        {
            this.storage = storage;
        }

        public string CreateToken(string? role, string? publisher = null, string name = "Test User", int lifetimeMinutes = 15, string? companyName = null)
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
            claims.Add(new Claim(ClaimTypes.Name, name));

            JwtSecurityToken token = new JwtSecurityToken(
                DefaultIssuer,
                DefaultAudience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(defaultKey), SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(storage);
                services.AddSingleton<IDocumentStorageClient, TestDocumentStorageClient>();
                services.AddSingleton<ILanguagesSource, DefaultLanguagesSource>();
                services.AddSingleton<ICodelistProviderClient, InternalCodelistProvider>();
                services.AddTransient<IFileStorageAccessPolicy, DefaultFileAccessPolicy>();
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

            return base.CreateHost(builder);
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
    }
}
