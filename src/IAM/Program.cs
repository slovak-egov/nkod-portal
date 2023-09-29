using IAM;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles.Infrastructure;
using Microsoft.EntityFrameworkCore;
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
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Util;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using ITfoxtec.Identity.Saml2.MvcCore;
using Microsoft.IdentityModel.Tokens.Saml2;
using Newtonsoft.Json;
using MySqlX.XDevAPI;
using System.Security.Policy;
using System.Xml.Schema;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

if (!int.TryParse(builder.Configuration["AccessTokenValidInMinutes"], out int accessTokenValidInMinutes))
{
    throw new Exception("Invalid value for AccessTokenValidInMinutes");
}

if (!int.TryParse(builder.Configuration["RefreshTokenValidInMinutes"], out int refreshTokenValidInMinutes))
{
    throw new Exception("Invalid value for RefreshTokenValidInMinutes");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("Connection string not found.");
    }

    options.UseMySQL(connectionString);
});

string? keyFile = builder.Configuration["AccessTokenKeyFile"];
string? password = builder.Configuration["AccessTokenKeyPassword"];

SigningCredentials? credentials = null;

if (keyFile is not null && password is not null )
{
    X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(keyFile), password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
    RSA? rsa = RSACertificateExtensions.GetRSAPrivateKey(certificate);
    if (rsa is not null)
    {
        RsaSecurityKey securityKey = new RsaSecurityKey(rsa);
        credentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha512);
    }
}

if (credentials is null)
{
    throw new Exception("Unable to create AccessTokenKey (invalid AccessTokenKeyFile or AccessTokenPassword)");
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton(credentials);

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = credentials.Key,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

builder.Services.AddSingleton(services =>
{
    Saml2Configuration saml2Configuration = new Saml2Configuration();
    builder.Configuration.Bind("Saml2", saml2Configuration);

    string? signingKey = builder.Configuration["Saml2:SigningCertificateFile"];
    string? signingPassword = builder.Configuration["Saml2:SigningCertificatePassword"];

    string? decryptionKey = builder.Configuration["Saml2:DecryptionCertificateFile"];
    string? decryptionPassword = builder.Configuration["Saml2:DecryptionCertificatePassword"];

    if (string.IsNullOrEmpty(signingKey) || string.IsNullOrEmpty(signingPassword) || string.IsNullOrEmpty(decryptionKey) || string.IsNullOrEmpty(decryptionPassword))
    {
        throw new Exception("Invalid configuration (SigningCertificateFile, SigningCertificatePassword, DecryptionCertificateFile, DecryptionCertificatePassword)");
    }

    saml2Configuration.SigningCertificate = new X509Certificate2(Convert.FromBase64String(signingKey), signingPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet); 
    saml2Configuration.DecryptionCertificate = new X509Certificate2(Convert.FromBase64String(decryptionKey), decryptionPassword, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

    saml2Configuration.AudienceRestricted = false;

    string? metadataUrl = builder.Configuration["Saml2:IdPMetadata"];
    if (!Uri.IsWellFormedUriString(metadataUrl, UriKind.Absolute))
    {
        throw new Exception("Invalid IdP metadata (Saml2:IdPMetadata)");
    }

    IHttpClientFactory? httpClientFactory = services.GetService<IHttpClientFactory>();
    EntityDescriptor entityDescriptor = new EntityDescriptor();
    entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, new Uri(metadataUrl)).GetAwaiter().GetResult();
    if (entityDescriptor.IdPSsoDescriptor != null)
    {
        saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
        saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
        saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;
        foreach (X509Certificate2 signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
        {
            if (signingCertificate.IsValidLocalTime())
            {
                saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
            }
        }
        if (saml2Configuration.SignatureValidationCertificates.Count <= 0)
        {
            throw new Exception("The IdP signing certificates has expired.");
        }
        if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
        {
            saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
        }
    }
    else
    {
        throw new Exception("IdPSsoDescriptor not loaded from metadata.");
    }

    return saml2Configuration;
});

builder.Services.AddSaml2(slidingExpiration: true);

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

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseSaml2();
app.UseAuthentication();
app.UseAuthorization();

async Task<TokenResult> CreateToken(ApplicationDbContext context, UserRecord user, IConfiguration configuration, SigningCredentials signingCredentials, bool hasExplicitDelegation, string? publisher = null, string? companyName = null)
{
    List<Claim> claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.GivenName, user.FirstName),
        new Claim(ClaimTypes.Surname, user.LastName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

    if (user.Email is not null)
    {
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
    }

    if (user.Role is not null)
    {
        string effectiveRole = user.Role;

        if (effectiveRole == "PublisherAdmin" && !hasExplicitDelegation)
        {
            effectiveRole = "Publisher";
        }

        claims.Add(new Claim(ClaimTypes.Role, effectiveRole));
    }

    publisher ??= user.Publisher;

    if (publisher is not null)
    {
        claims.Add(new Claim("Publisher", publisher));
    }

    if (companyName is not null)
    {
        claims.Add(new Claim("CompanyName", companyName));
    }

    DateTimeOffset expires = DateTimeOffset.Now.AddMinutes(accessTokenValidInMinutes);

    JwtSecurityToken jwtToken = new JwtSecurityToken(
        configuration["Jwt:Issuer"],
        configuration["Jwt:Audience"],
        claims,
        expires: expires.LocalDateTime,
        signingCredentials: signingCredentials);

    string token = new JwtSecurityTokenHandler().WriteToken(jwtToken);

    if (refreshTokenValidInMinutes > 0 && string.IsNullOrEmpty(user.RefreshToken) || !user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime < DateTimeOffset.Now)
    {
        byte[] refreshTokenBytes = new byte[64];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(refreshTokenBytes);
        }
        string refreshToken = Convert.ToBase64String(refreshTokenBytes);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTimeOffset.Now.AddMinutes(refreshTokenValidInMinutes);
        await context.SaveChangesAsync();
    }

    return new TokenResult
    {
        Token = token,
        Expires = expires,
        RefreshToken = user.RefreshToken
    };
}

app.MapGet("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, int? limit, int? offset, string? id) =>
{
    offset ??= 0;

    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        IQueryable<UserRecord> query = context.Users.Where(u => u.Publisher == publisherId);

        if (!string.IsNullOrEmpty(id))
        {
            query = query.Where(u => u.Id == id);
        }

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
                Role = record.Role,
                FirstName = record.FirstName,
                LastName = record.LastName
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
            SaveResult result = new SaveResult();
            ValidationResults validationResults = input.Validate();
            if (validationResults.IsValid && input.IdentificationNumber is not null)
            {
                UserRecord record = new UserRecord
                {
                    Id = input.IdentificationNumber,
                    Email = input.Email ?? string.Empty,
                    Role = input.Role,
                    Publisher = publisherId,
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                };
                context.Users.Add(record);
                await context.SaveChangesAsync();
                result.Id = record.Id;
                result.Success = true;
            }
            else
            {
                result.Errors = validationResults;
            }

            return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
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
            SaveResult result = new SaveResult();
            UserRecord? record = await context.FindUser(input.Id, publisherId);
            if (record is not null)
            {
                ValidationResults validationResults = input.Validate();
                if (validationResults.IsValid)
                {
                    record.Email = input.Email ?? string.Empty;
                    record.Role = input.Role;
                    record.FirstName = input.FirstName ?? string.Empty;
                    record.LastName = input.LastName ?? string.Empty;
                    await context.SaveChangesAsync();
                    result.Id = record.Id;
                    result.Success = true;
                }
                else
                {
                    result.Errors ??= new Dictionary<string, string>();
                    result.Errors["generic"] = "Not found";
                }
            }
            return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
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

            string? publisher = record.Publisher;

            if (user.IsInRole("Superadmin"))
            {
                string? publisherFromToken = user.FindFirstValue("Publisher");
                if (publisherFromToken is not null)
                {
                    publisher = publisherFromToken;
                }
            }

            return Results.Ok(new UserInfo
            {
                Id = record.Id,
                Publisher = publisher,
                Role = record.Role,
                FirstName = firstName ?? record.FirstName,
                LastName = lastName ?? record.LastName,
                Email = record.Email,
                CompanyName = user.FindFirstValue("CompanyName"),
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

                        bool hasExplicitDelegation = principal.IsInRole("PublisherAdmin");

                        if (areEqual && request.RefreshToken.Length == refreshTokenLen)
                        {
                            return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, hasExplicitDelegation));
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
                return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, false, publisher));
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

app.MapGet("/login", ([FromServices] Saml2Configuration saml2Configuration, IConfiguration configuration) =>
{
    string? returnUrl = configuration["Saml2:ReturnUrl"];
    if (string.IsNullOrEmpty(returnUrl))
    { 
        return Results.Problem("Invalid configuration (A)");
    }

    Saml2RedirectBinding binding = new Saml2RedirectBinding();
    binding.SetRelayStateQuery(new Dictionary<string, string> { { "ReturnUrl", returnUrl } });

    Saml2AuthnRequest request = new Saml2AuthnRequest(saml2Configuration);
    request.Issuer = configuration["Saml2:EntityId"];

    Saml2RedirectBinding redirectBinding = binding.Bind(request);
    return Results.Ok(new DelegationAuthorizationResult
    {
        RedirectUrl = redirectBinding.RedirectLocation.OriginalString
    });
});

app.MapPost("/consume", async ([FromServices] Saml2Configuration saml2Configuration, [FromServices] ApplicationDbContext context, HttpRequest request, [FromServices] SigningCredentials signingCredentials, IConfiguration configuration) =>
{
    Saml2PostBinding binding = new Saml2PostBinding();
    Saml2AuthnResponse saml2AuthnResponse = new Saml2AuthnResponse(saml2Configuration);

    ITfoxtec.Identity.Saml2.Http.HttpRequest genericRequest = request.ToGenericHttpRequest();

    binding.ReadSamlResponse(genericRequest, saml2AuthnResponse);
    if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
    {
        return Results.BadRequest($"SAML Response status: {saml2AuthnResponse.Status}");
    }
    binding.Unbind(genericRequest, saml2AuthnResponse);


    string? id = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.UPVSIdentityID")?.Value;
    string? firstName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.FirstName")?.Value;
    string? lastName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.LastName")?.Value;
    string? email = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.Email")?.Value;
    string? ico = saml2AuthnResponse.ClaimsIdentity.FindFirst("Subject.ICO")?.Value;
    string? companyName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Subject.FormattedName")?.Value;

    string? publisher = null;
    bool hasExplicitDelegation = false;
    if (!string.IsNullOrEmpty(ico))
    {
        publisher = $"https://data.gov.sk/id/legal-subject/{ico}";
        hasExplicitDelegation = true;
    }

    if (!string.IsNullOrEmpty(id))
    {
        UserRecord? user = await context.GetOrCreateUser(id, firstName, lastName, email, publisher).ConfigureAwait(false);
        if (user is not null)
        {
            return Results.Ok(await CreateToken(context, user, configuration, signingCredentials, hasExplicitDelegation, companyName: companyName));
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