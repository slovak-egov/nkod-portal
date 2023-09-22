using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;
using NkodSk.RdfFileStorage;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestBase;

namespace DocumentStorageApi.Test
{
    public class DocumentStorageApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly IFileStorage storage;

        private const string DefaultIssuer = "http://test/";

        private const string DefaultAudience = "http://test/";

        private readonly byte[] defaultKey = RandomNumberGenerator.GetBytes(32);

        public DocumentStorageApplicationFactory(IFileStorage storage)
        {
            this.storage = storage;
        }

        public IHttpContextValueAccessor CreateAccessor(string? role = null, string? publisher = null, string? id = null)
        {
            string? token = null;
            if (!string.IsNullOrEmpty(role))
            {
                token = CreateToken(role, publisher);
            }
            return new StaticHttpContextValueAccessor(publisher, token, role, id);
        }

        public string CreateToken(string? role, string? publisher = null, string name = "Test User", int lifetimeMinutes = 15)
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
    }
}
