using IAM;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.Util;
using ITfoxtec.Identity.Saml2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Saml2.Schemas;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IAMClient;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using NkodSk.Abstractions;
using System.Drawing.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//    if (string.IsNullOrEmpty(connectionString))
//    {
//        throw new Exception("Connection string not found.");
//    }

//    options.UseMySQL(connectionString);
//});

builder.Services.AddHttpClient();

builder.Services.AddSingleton(s =>
{
    RSA rsa = RSA.Create();
    rsa.ImportFromPem(builder.Configuration["Jwt:Key"]);
    RsaSecurityKey securityKey = new RsaSecurityKey(rsa);
    return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512);
});

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
).AddJwtBearer(o =>
{
    RSA rsa = RSA.Create();
    rsa.ImportFromPem(builder.Configuration["Jwt:Key"]);

    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

//builder.Services.AddSingleton(services =>
//{
//    Saml2Configuration saml2Configuration = new Saml2Configuration();
//    builder.Configuration.Bind("Saml2", saml2Configuration);

//    saml2Configuration.SigningCertificate = CertificateUtil.Load(builder.Configuration["Saml2:SigningCertificateFile"], builder.Configuration["Saml2:SigningCertificatePassword"], X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
//    saml2Configuration.AllowedAudienceUris.Add(saml2Configuration.Issuer);

//    var httpClientFactory = services.GetService<IHttpClientFactory>();
//    var entityDescriptor = new EntityDescriptor();
//    entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, new Uri(builder.Configuration["Saml2:IdPMetadata"])).GetAwaiter().GetResult();
//    if (entityDescriptor.IdPSsoDescriptor != null)
//    {
//        saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
//        saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
//        saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;
//        foreach (var signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
//        {
//            if (signingCertificate.IsValidLocalTime())
//            {
//                saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
//            }
//        }
//        if (saml2Configuration.SignatureValidationCertificates.Count <= 0)
//        {
//            throw new Exception("The IdP signing certificates has expired.");
//        }
//        if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
//        {
//            saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
//        }
//    }
//    else
//    {
//        throw new Exception("IdPSsoDescriptor not loaded from metadata.");
//    }
//    return saml2Configuration;
//});

//builder.Services.AddSaml2(slidingExpiration: true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IAM API",
        Version = "v1",
    });
    options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

//app.UseSaml2();
app.UseAuthentication();
app.UseAuthorization();

async Task<TokenResult> CreateToken(ApplicationDbContext context, UserRecord user, IConfiguration configuration, SigningCredentials signingCredentials, ClaimsPrincipal claimsPrincipal, string? publisher = null)
{    
    List<Claim> claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.GivenName, claimsPrincipal.FindFirstValue(ClaimTypes.GivenName) ?? user.FirstName),
        new Claim(ClaimTypes.Surname, claimsPrincipal.FindFirstValue(ClaimTypes.Surname) ?? user.LastName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    if (user.Role is not null)
    {
        claims.Add(new Claim(ClaimTypes.Role, user.Role));
    }

    publisher ??= user.Publisher;

    if (publisher is not null)
    {
        claims.Add(new Claim("Publisher", publisher));
    }

    JwtSecurityToken jwtToken = new JwtSecurityToken(
        configuration["Jwt:Issuer"],
        configuration["Jwt:Audience"],
        claims,
        expires: DateTime.Now.AddMinutes(30),
        signingCredentials: signingCredentials);

    string token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

    if (string.IsNullOrEmpty(user.RefreshToken) || !user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime < DateTimeOffset.Now)
    {
        byte[] refreshTokenBytes = new byte[64];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(refreshTokenBytes);
        }
        string refreshToken = Convert.ToBase64String(refreshTokenBytes);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTimeOffset.Now.AddMonths(1);
        await context.SaveChangesAsync();
    }

    return new TokenResult
    {
        Token = token,
        RefreshToken = user.RefreshToken
    };
}

app.MapGet("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, int? limit, int? offset) =>
{
    offset ??= 0;

    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        IQueryable<UserRecord> query = context.Users.Where(u => u.Publisher == publisherId);

        int total = query.Count();

        if (offset > 0)
        {
            query = query.Skip(offset.Value);
        }

        query = query.OrderBy(u => u.Id);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        List<UserRecord> records = await query.ToListAsync();
        List<PersistentUserInfo> users = new List<PersistentUserInfo>(records.Count);
        foreach (UserRecord record in records)
        {
            users.Add(new PersistentUserInfo
            {
                Id = record.Id,
                Email = record.Email,
                Role = record.Role
            });
        }

        UserInfoResult result = new UserInfoResult(users, total);
        return Results.Ok(result);
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapPost("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromBody] NewUserInput? input) =>
{
    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        if (input is not null)
        {
            UserRecord record = new UserRecord
            {
                Id = Guid.NewGuid().ToString(),
                Email = input.Email ?? string.Empty,
                Role = input.Role,
                Publisher = publisherId,
                FirstName = input.FirstName,
                LastName = input.LastName,
            };
            context.Users.Add(record);
            await context.SaveChangesAsync();

            SaveResult result = new SaveResult
            {
                Id = record.Id,
                Success = true
            };

            return Results.Ok(result);
        }
        else
        {
            return Results.BadRequest();
        }
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapPut("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromBody] EditUserInput? input) =>
{
    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        if (input?.Id is not null)
        {
            UserRecord? record = await context.FindUser(input.Id, publisherId);
            if (record is not null)
            {
                record.Email = input.Email ?? string.Empty;
                record.Role = input.Role;
                await context.SaveChangesAsync();

                SaveResult result = new SaveResult
                {
                    Id = record.Id,
                    Success = true
                };

                return Results.Ok(result);
            }
            else
            {
                return Results.NotFound();
            }
        }
        else
        {
            return Results.BadRequest();
        }
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapDelete("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromQuery] string? id) =>
{
    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        if (!string.IsNullOrEmpty(id))
        {
            UserRecord? record = await context.FindUser(id, publisherId);
            if (record is not null)
            {
                context.Users.Remove(record);
                await context.SaveChangesAsync();
                return Results.Ok();
            }
            else
            {
                return Results.NotFound();
            }
        }
        else
        {
            return Results.BadRequest();
        }
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapGet("/user-info", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user) =>
{
    string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (id is not null)
    {
        UserRecord? record = await context.Users.FindAsync(id);

        if (record is not null)
        {
            string? firstName = user.FindFirstValue(ClaimTypes.GivenName);
            string? lastName = user.FindFirstValue(ClaimTypes.Surname);

            return Results.Ok(new UserInfo
            {
                Id = record.Id,
                Publisher = record.Publisher,
                Role = record.Role,
                FirstName = firstName ?? record.FirstName,
                LastName = lastName ?? record.LastName,
                Email = record.Email,
            });
        }
        else
        {
            return Results.NotFound();
        }
    }
    else
    {
        return Results.NotFound();
    }
});

app.MapGet("/login", async ([FromServices] Saml2Configuration saml2Configuration) =>
{
    
});

app.MapPost("/consume", ([FromServices] Saml2Configuration saml2Configuration, IHttpContextAccessor httpContextAccessor) =>
{
    
});

app.MapPost("/refresh", async ([FromServices] ApplicationDbContext context, [FromBody] RefreshTokenRequest? request, [FromServices] SigningCredentials signingCredentials, [FromServices] IConfiguration configuration, HttpContext httpContext) =>
{
    if (!string.IsNullOrEmpty(request?.RefreshToken) && !string.IsNullOrEmpty(request.AccessToken))
    {
        TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = configuration["Jwt:Audience"],
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingCredentials.Key,
            ValidateLifetime = false
        };

        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            ClaimsPrincipal principal = tokenHandler.ValidateToken(request.AccessToken, tokenValidationParameters, out SecurityToken securityToken);
            if (principal is not null)
            {
                string? id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (id is not null)
                {
                    UserRecord? record = await context.Users.FindAsync(id);
                    if (record is not null && record.RefreshTokenExpiryTime.HasValue && record.RefreshTokenExpiryTime.Value > DateTimeOffset.Now)
                    {
                        int refreshTokenLen = record.RefreshToken?.Length ?? 0;
                        bool areEqual = true;

                        for (int i = 0; i < request.RefreshToken.Length; i++)
                        {
                            char expected = i < refreshTokenLen ? record.RefreshToken?[i] ?? '\0' : '\0';
                            char actual = request.RefreshToken[i];
                            if (actual != expected)
                            {
                                areEqual = false;
                            }
                        }

                        if (areEqual && request.RefreshToken.Length == refreshTokenLen)
                        {
                            return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, principal));
                        }
                    }
                }
            }
        }
        catch
        {
            //ignore
        }
    }

    return Results.Forbid();
});

app.MapGet("/logout", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user) =>
{
    string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (id is not null)
    {
        UserRecord? record = await context.Users.FindAsync(id);
        if (record is not null)
        {
            record.RefreshToken = null;
            record.RefreshTokenExpiryTime = null;
            await context.SaveChangesAsync();
        }
    }

    return new DelegationAuthorizationResult();
});

app.MapPost("/delegate-publisher", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromServices] SigningCredentials signingCredentials, [FromServices] IConfiguration configuration, [FromQuery] string? publisher) =>
{
    if (user.IsInRole("Superadmin"))
    {
        string? id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id is not null)
        {
            UserRecord? record = await context.Users.FindAsync(id);
            if (record is not null)
            {
                return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, user, publisher));
            }
            else
            {
                return Results.Forbid();
            }
        }
        else
        {
            return Results.Forbid();
        }        
    }
    else
    {
        return Results.Forbid();
    }
});

app.Run();

public partial class Program { }