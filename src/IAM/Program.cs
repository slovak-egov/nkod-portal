﻿using IAM;
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
using System.Security.Policy;
using System.Xml.Schema;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights;
using ITfoxtec.Identity.Saml2.Claims;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Google.Protobuf.WellKnownTypes;
using System.Text.RegularExpressions;
using System.Web;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Abstractions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using System.Net;
using Microsoft.Extensions.Options;

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
        saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.FirstOrDefault()?.Location;
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

builder.Services.Configure<MainConfigurationOptions>(builder.Configuration.GetSection("Main"));

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
    options.EnableAdaptiveSampling = false;
});
builder.Services.AddSingleton<ITelemetryInitializer, RequestTelementryInitializer>();

string? frontendUrl = builder.Configuration["FrontendUrl"];
EmailOptions? emailOptions = builder.Configuration.GetSection("EmailOptions").Get<EmailOptions>();
builder.Services.AddSingleton<IEmailService>(_ => new SmtpEmailService(emailOptions?.Host, emailOptions?.Port ?? 25, emailOptions?.Username, emailOptions?.Password, emailOptions?.UseSsl ?? false, emailOptions?.FromAddress, emailOptions?.FromName));

builder.Logging.AddConsole();

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

async Task<TokenResult> CreateToken(ApplicationDbContext context, UserRecord? user, IConfiguration configuration, SigningCredentials signingCredentials, bool hasExplicitDelegation, IEnumerable<Claim>? customClaims, string? publisher = null, string? companyName = null)
{
    List<Claim> claims = new List<Claim>();

    if (user is not null)
    {
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
        claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

        if (!string.IsNullOrEmpty(user.FormattedName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.FormattedName));
        }
    }

    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

    if (user?.Email is not null)
    {
        claims.Add(new Claim(ClaimTypes.Email, user.Email));
    }

    if (user?.Role is not null)
    {
        string effectiveRole = user.Role;

        if (effectiveRole == "PublisherAdmin" && !hasExplicitDelegation)
        {
            effectiveRole = "Publisher";
        }

        claims.Add(new Claim(ClaimTypes.Role, effectiveRole));
    }

    publisher ??= user?.Publisher;

    if (publisher is not null)
    {
        claims.Add(new Claim("Publisher", publisher));
    }

    if (companyName is not null)
    {
        claims.Add(new Claim("CompanyName", companyName));
    }

    if (customClaims is not null)
    {
        foreach (Claim claim in customClaims)
        {
            if (!claims.Any(c => c.Type == claim.Type))
            {
                claims.Add(claim);
            }
        }
    }

    DateTimeOffset expires = DateTimeOffset.Now.AddMinutes(accessTokenValidInMinutes);

    string token = CreateJwtToken(configuration, signingCredentials, claims, expires);

    if (user is not null)
    {
        if (refreshTokenValidInMinutes > 0 && (string.IsNullOrEmpty(user.RefreshToken) || !user.RefreshTokenExpiryTime.HasValue || user.RefreshTokenExpiryTime < DateTimeOffset.Now))
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
    }

    return new TokenResult
    {
        Token = token,
        Expires = expires,
        RefreshTokenAfter = expires.AddMinutes(-5),
        RefreshTokenInSeconds = Math.Max(accessTokenValidInMinutes - 5, 0) * 60,
        RefreshToken = user?.RefreshToken
    };
}

string CreateJwtToken(IConfiguration configuration, SigningCredentials signingCredentials, List<Claim> claims, DateTimeOffset expires)
{
    JwtSecurityToken jwtToken = new JwtSecurityToken(
        configuration["Jwt:Issuer"],
        configuration["Jwt:Audience"],
        claims,
        expires: expires.LocalDateTime,
        signingCredentials: signingCredentials);
    return new JwtSecurityTokenHandler().WriteToken(jwtToken);
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
                LastName = record.LastName,
                IsActive = record.IsActive,
                InvitationExpiresAt = record.InvitedAt?.AddHours(48)
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

app.MapPost("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromBody] NewUserInput? input, IOptions<MainConfigurationOptions> mainConfigurationOptions, [FromServices] IEmailService emailService) =>
{
    string? publisherId = user.GetAuthorizedPublisherId();

    if (!string.IsNullOrEmpty(publisherId))
    {
        if (input is not null)
        {
            UserSaveResult result = new UserSaveResult();
            ValidationResults validationResults = input.Validate();
            if (validationResults.IsValid)
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

                byte[] invitationTokenBytes = new byte[64];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(invitationTokenBytes);
                }

                record.InvitationToken = string.Concat(Array.ConvertAll(invitationTokenBytes, b => b.ToString("x2")));
                record.InvitedAt = DateTimeOffset.Now;
                if (Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid userId))
                {
                    record.InvitedBy = userId;
                }

                string? password = null;

                if (mainConfigurationOptions.Value.UsePasswordForPublisherAccounts)
                {
                    password = record.CreateRandomPassword();
                    record.InvitationToken = null;
                    record.IsActive = true;
                }

                context.Users.Add(record);
                await context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(password))
                {
                    string text = "Vážený používateľ,<br>" +
                        "bolo Vám zriadené konto na testovacom portáli data-test.slovensko.sk<br>" +
                        "Prihlasovacie meno: " + HttpUtility.HtmlEncode(record.Email) + "<br>" +
                        "Heslo: " + HttpUtility.HtmlEncode(password) + "<br>" +
                        "<br>" +
                        "Tím centrálneho portálu otvorených dát data.slovensko.sk";

                    await emailService.SendEmail(record.Email, "Konto na testovacom portáli data-test.slovensko.sk", text);
                }

                result.Id = record.Id;
                result.Success = true;
                result.InvitationToken = record.InvitationToken;
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
            UserSaveResult result = new UserSaveResult();
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
                    record.InvitedAt = DateTimeOffset.Now;
                    await context.SaveChangesAsync();
                    result.Id = record.Id;
                    result.Success = true;
                    result.InvitationToken = record.InvitationToken;
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
                AuthorizationMethod = user.FindFirstValue(ClaimTypes.AuthenticationMethod),
                FormattedName = record.FormattedName
            });
        }
        else
        {
            return Results.Ok(new UserInfo
            {
                Id = id,
                Publisher = null,
                Role = null,
                FirstName = user.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                LastName = user.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                Email = null,
                CompanyName = null,
            });
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
                            string? publisher = null;

                            if (principal.IsInRole("Superadmin"))
                            {
                                publisher = principal.FindFirst("Publisher")?.Value;
                            }

                            return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, hasExplicitDelegation, principal.Claims, publisher: publisher));
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

    return Results.Ok(new TokenResult());
});

app.MapGet("/logout", [Authorize] async ([FromServices] ApplicationDbContext context, HttpRequest request, ClaimsPrincipal user, [FromServices] Saml2Configuration saml2Configuration, [FromServices] IConfiguration configuration, [FromServices] ILogger<Program> logger) =>
{
    string? method = user.FindFirstValue(ClaimTypes.AuthenticationMethod);

    if (string.Equals(method, "Native") || string.Equals(method, "Google"))
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

        return Results.Ok(new DelegationAuthorizationResult
        {
            DoLogout = true
        });
    }

    string? content = request.Query["SAMLResponse"];

    if (!string.IsNullOrEmpty(content))
    {
        Saml2RedirectBinding binding = new Saml2RedirectBinding();
        Saml2LogoutResponse saml2AuthnResponse = new Saml2LogoutResponse(saml2Configuration);

        ITfoxtec.Identity.Saml2.Http.HttpRequest genericRequest = request.ToGenericHttpRequest();

        binding.ReadSamlResponse(genericRequest, saml2AuthnResponse);
        if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
        {
            logger.LogInformation($"SAML Response status: {saml2AuthnResponse.Status}");
            return Results.BadRequest($"SAML Response status: {saml2AuthnResponse.Status}");
        }
        binding.Unbind(genericRequest, saml2AuthnResponse);
        logger.LogInformation(saml2AuthnResponse.XmlDocument.OuterXml);

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

        return Results.Ok(new DelegationAuthorizationResult
        {
            DoLogout = true
        });
    } 
    else
    {
        content = request.Query["SAMLRequest"];

        if (!string.IsNullOrEmpty(content))
        {
            Saml2LogoutRequest logoutRequest = new Saml2LogoutRequest(saml2Configuration);
            Saml2RedirectBinding binding = new Saml2RedirectBinding();

            ITfoxtec.Identity.Saml2.Http.HttpRequest genericRequest = request.ToGenericHttpRequest();

            binding.ReadSamlRequest(genericRequest, logoutRequest);
            binding.Unbind(genericRequest, logoutRequest);
            logger.LogInformation(logoutRequest.XmlDocument.OuterXml);

            Saml2RedirectBinding responseBinding = new Saml2RedirectBinding();
            responseBinding.RelayState = binding.RelayState;
            Saml2LogoutResponse response = new Saml2LogoutResponse(saml2Configuration)
            {
                InResponseToAsString = logoutRequest.IdAsString,
                Status = Saml2StatusCodes.Success,
                Issuer = configuration["Saml2:EntityId"]
            };

            Saml2RedirectBinding redirectBinding = responseBinding.Bind(response);

            logger.LogInformation(redirectBinding.XmlDocument.OuterXml);

            return Results.Ok(new DelegationAuthorizationResult
            {
                RedirectUrl = redirectBinding.RedirectLocation.OriginalString,
                DoLogout = true
            });
        }
        else
        {
            Saml2RedirectBinding binding = new Saml2RedirectBinding();
            CustomLogoutRequest logoutRequest = new CustomLogoutRequest(saml2Configuration, user);
            logoutRequest.Issuer = configuration["Saml2:EntityId"];
            logoutRequest.NameId.NameQualifier = configuration["Saml2:Issuer"];
            logoutRequest.NameId.SPNameQualifier = configuration["Saml2:EntityId"];

            Saml2RedirectBinding redirectBinding = binding.Bind(logoutRequest);

            logger.LogInformation(logoutRequest.XmlDocument.OuterXml);

            return Results.Ok(new DelegationAuthorizationResult
            {
                RedirectUrl = redirectBinding.RedirectLocation.OriginalString
            });
        }        
    }
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
                return Results.Ok(await CreateToken(context, record, configuration, signingCredentials, false, user.Claims, publisher));
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

app.MapGet("/login", ([FromServices] Saml2Configuration saml2Configuration, [FromQuery] string? method, IConfiguration configuration, [FromServices] ILogger<Program> logger) =>
{
    if (string.Equals(method, "Google", StringComparison.OrdinalIgnoreCase))
    {
        UriBuilder redirectUriBuilder = new UriBuilder(frontendUrl ?? string.Empty);
        redirectUriBuilder.Path = "/signin-google";

        string baseUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        Dictionary<string, string?> parameters = new Dictionary<string, string?>
        {
            { "client_id", configuration["Authentication:Google:ClientId"] },
            { "response_type", "code" },
            { "access_type", "online" },
            { "scope", "email profile" },
            { "redirect_uri", redirectUriBuilder.ToString() },
            { "state", "google" }
        };

        UriBuilder ub = new UriBuilder(baseUrl);
        ub.Query = string.Join("&", parameters.Select(k => $"{HttpUtility.UrlEncode(k.Key)}={HttpUtility.UrlEncode(k.Value)}"));

        return Results.Ok(new DelegationAuthorizationResult
        {
            RedirectUrl = ub.ToString()
        });
    }

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

    logger.LogInformation(request.XmlDocument.OuterXml);
    
    return Results.Ok(new DelegationAuthorizationResult
    {
        RedirectUrl = redirectBinding.RedirectLocation.OriginalString
    });
});

app.MapPost("/login", async ([FromBody] LoginInput? input, [FromServices] ApplicationDbContext context, [FromServices] SigningCredentials signingCredentials, IOptions<MainConfigurationOptions> mainConfigurationOptions, IConfiguration configuration) =>
{
    if (!string.IsNullOrEmpty(input?.Email) && !string.IsNullOrEmpty(input?.Password))
    {
        UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Email == input.Email && u.IsActive);
        if (user is not null && !string.IsNullOrWhiteSpace(user.Password) && user.VerifyPassword(input.Password))
        {
            if (user.Role == "CommunityUser" || mainConfigurationOptions.Value.UsePasswordForPublisherAccounts)
            {
                List<Claim> customClaims = new List<Claim> { new Claim(ClaimTypes.AuthenticationMethod, "Native") };

                return Results.Ok(await CreateToken(context, user, configuration, signingCredentials, mainConfigurationOptions.Value.UsePasswordForPublisherAccounts, customClaims));
            }
        }
    }

    return Results.Forbid();
});

string CreateRandomString()
{
    byte[] buffer = new byte[32];
    using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(buffer);
    }
    return Convert.ToBase64String(buffer).Trim('=');
}

app.MapPost("/register", async ([FromBody] UserRegistrationInput? input, [FromServices] ApplicationDbContext context, [FromServices] IEmailService emailService) =>
{
    SaveResult result = new SaveResult();

    input ??= new UserRegistrationInput();

    ValidationResults validationResults = input.Validate();
    if (validationResults.IsValid)
    {
        string password = input.Password?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(input.Email) && !string.IsNullOrWhiteSpace(password) && password.Length >= UserRegistrationInput.MinimalPasswordLength && !string.IsNullOrWhiteSpace(input.FirstName) && !string.IsNullOrWhiteSpace(input.LastName))
        {
            using IDbContextTransaction tx = await context.Database.BeginTransactionAsync();
            if (!await context.Users.AnyAsync(u => u.Email == input.Email))
            {
                TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                DateTimeOffset expiryTimeUtc = DateTimeOffset.UtcNow.AddDays(2);
                DateTimeOffset expiryTimeOffset = TimeZoneInfo.ConvertTime(expiryTimeUtc, tz);

                UserRecord user = new UserRecord
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = input.Email,
                    Role = "CommunityUser",
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    IsActive = false,
                    RegistrationSourceIpAddress = "1",
                    Registered = DateTimeOffset.Now,
                    ActivationToken = CreateRandomString(),
                    ActivationTokenExpiryTime = expiryTimeUtc,
                    FormattedName = $"{input.FirstName} {input.LastName}"
                };

                user.SetPassword(password);

                context.Users.Add(user);
                await context.SaveChangesAsync();
                tx.Commit();

                string link = $"{frontendUrl}potvrdenie-registracie?id={HttpUtility.UrlEncode(user.Id)}&token={HttpUtility.UrlEncode(user.ActivationToken)}";
                string encodedLink = HttpUtility.HtmlEncode(link);

                string text = "Vážený používateľ,<br>" +
                        "na získanie prístupových práv pre aktívneho používateľa portálu data.slovensko.sk, prosím, " +
                        $"kliknite na nasledujúci odkaz do {expiryTimeOffset:d. M. yyyy, H:mm}: <a href=\"{encodedLink}\">{encodedLink}</a>. Tento odkaz Vám umožní využívať možnosti, ktoré ponúka portál pre zaregistrovaných používateľov.<br><br>" +
                        "Po potvrdení Vám budú poskytnuté prístupové práva na zadanie podnetu, registráciu aplikácie, vloženie komentára a ďalšie funkcionality.<br><br>" +
                        "Pokiaľ máte akékoľvek otázky alebo potrebujete pomoc, neváhajte nás kontaktovať na e-maili opendata@mirri.gov.sk.<br><br>" +
                        "Tešíme sa na spoluprácu a Váš príspevok k rozvoju otvorených dát v Slovenskej republike.<br><br>" +
                        "S pozdravom<br>" +
                        "Tím centrálneho portálu otvorených dát data.slovensko.sk";

                await emailService.SendEmail(user.Email, "Aktivácia žiadosti o vytvorenie konta", text);
                
                result.Id = user.Id;
                result.Success = true;
            }
            else
            {
                validationResults.AddError(nameof(input.Email), "E-mail je už zaregistrovaný");
            }
        }
    }
    result.Errors = validationResults;
    return Results.Ok(result);
});

app.MapPost("/activation", async ([FromBody] ActivationInput? input, [FromServices] ApplicationDbContext context) =>
{
    SaveResult result = new SaveResult();
    ValidationResults results = new ValidationResults();

    if (!string.IsNullOrEmpty(input?.Id) && !string.IsNullOrEmpty(input.Token))
    {
        UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.Id);
        if (user is not null && !string.IsNullOrEmpty(user.ActivationToken))
        {
            if (user.ActivationTokenExpiryTime.HasValue && user.ActivationTokenExpiryTime.Value >= DateTimeOffset.Now)
            {
                if (string.Equals(user.ActivationToken, input.Token, StringComparison.Ordinal))
                {
                    user.ActivationToken = null;
                    user.ActivationTokenExpiryTime = null;
                    user.IsActive = true;
                    user.ActivatedAt = DateTimeOffset.Now;
                    await context.SaveChangesAsync();

                    result.Id = user.Id;
                    result.Success = true;
                }
            }
            else
            {
                results.AddError(nameof(input.Token), "Overenie aktivácie exspirovalo");
            }
        }
        else
        {
            results.AddError(nameof(input.Token), "Overenie aktivácie nie je platné");
        }
    }
    else
    {
        results.AddError(nameof(input.Token), "Overenie aktivácie nie je platné");
    }

    result.Errors = results;

    return Results.Ok(result);
});

app.MapPost("/recovery", async ([FromBody] PasswordRecoveryInput? input, [FromServices] ApplicationDbContext context, [FromServices] IEmailService emailService) =>
{
    SaveResult result = new SaveResult();

    input ??= new PasswordRecoveryInput();

    ValidationResults validationResults = input.Validate();
    if (validationResults.IsValid)
    {
        if (!string.IsNullOrEmpty(input?.Email))
        {
            UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Email == input.Email && u.IsActive);
            if (user is not null && !string.IsNullOrEmpty(user.Password) && !string.IsNullOrEmpty(user.Email))
            {
                if (user.RecoveryTokenSentTimes < 3)
                {
                    if (string.IsNullOrEmpty(user.RecoveryToken))
                    {
                        user.RecoveryToken = CreateRandomString();
                    }

                    TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                    DateTimeOffset expiryTimeUtc = DateTimeOffset.UtcNow.AddDays(2);
                    DateTimeOffset expiryTimeOffset = TimeZoneInfo.ConvertTime(expiryTimeUtc, tz);

                    user.RecoveryTokenExpiryTime = expiryTimeUtc;
                    user.RecoveryTokenSentTimes++;
                    await context.SaveChangesAsync();

                    string link = $"{frontendUrl}obnova-hesla?id={HttpUtility.UrlEncode(user.Id)}&token={HttpUtility.UrlEncode(user.RecoveryToken)}";
                    string encodedLink = HttpUtility.HtmlEncode(link);

                    string text = "Vážený používateľ,<br>" +
                       "zistili sme, že ste požiadali o obnovu hesla k Vášmu účtu na portáli data.slovensko.sk.<br><br>" +
                       $"Ak ste zabudli Vaše heslo, môžete si vygenerovať nové kliknutím na nasledujúci odkaz: <a href=\"{encodedLink}\">{encodedLink}</a>.<br><br>" +
                       $"Prosím, pamätajte si, že tento odkaz bude platný iba počas nasledujúcich dvoch dní do {expiryTimeOffset:d. M. yyyy, H:mm}, preto nezabudnite na obnovu hesla čo najskôr.<br><br>" +
                       "Ak ste nežiadali o obnovu hesla alebo Ste si žiadny problém s heslom nevšimli, môžete tento e-mail ignorovať. Vaše heslo zostane nezmenené.<br><br>" +
                       "Ak máte akékoľvek ďalšie otázky alebo potrebujete pomoc, neváhajte nás kontaktovať na emaili opendata@mirri.gov.sk<br><br>" +
                       "S pozdravom,<br>" +
                       "Tím centrálneho portálu otvorených dát data.slovensko.sk";

                    await emailService.SendEmail(user.Email, "Aktivácia žiadosti o obnovu hesla", text);

                    result.Id = user.Id;
                    result.Success = true;
                }
                else
                {
                    validationResults.AddError(nameof(input.Email), "Pre túto e-mailovú adresu boli odoslané príliš veľa požiadaviek na obnovu hesla");
                }
            }
            else
            {
                validationResults.AddError(nameof(input.Email), "Konto pre zadanú e-mailovú adresu nebolo nájdené");
            }
        }
    }
     
    result.Errors = validationResults;

    return Results.Ok(result);
});

app.MapPost("/recovery-activation", async ([FromBody] PasswordRecoveryConfirmationInput? input, [FromServices] ApplicationDbContext context, [FromServices] IEmailService emailService) =>
{
    SaveResult result = new SaveResult();
    ValidationResults results = new ValidationResults();

    if (!string.IsNullOrEmpty(input?.Id) && !string.IsNullOrEmpty(input.Token) && !string.IsNullOrEmpty(input.Password))
    {
        UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == input.Id && u.IsActive);
        if (user is not null && !string.IsNullOrEmpty(user.RecoveryToken))
        {
            if (user.RecoveryTokenExpiryTime.HasValue && user.RecoveryTokenExpiryTime.Value >= DateTimeOffset.Now)
            {
                if (string.Equals(user.RecoveryToken, input.Token, StringComparison.Ordinal))
                {
                    user.RecoveryToken = null;
                    user.RecoveryTokenSentTimes = 0;
                    user.RecoveryTokenExpiryTime = null;
                    user.SetPassword(input.Password);
                    await context.SaveChangesAsync();

                    result.Id = user.Id;
                    result.Success = true;
                }
            }
            else
            {
                results.AddError(nameof(input.Token), "Overenie aktivácie exspirovalo");
            }
        }
        else
        {
            results.AddError(nameof(input.Token), "Overenie aktivácie nie je platné");
        }
    }
    else
    {
        results.AddError(nameof(input.Token), "Overenie aktivácie nie je platné");
    }

    result.Errors = results;

    return Results.Ok(result);
});

app.MapPost("/change-password", [Authorize] async ([FromBody] PasswordChangeInput? input, [FromServices] ApplicationDbContext context, ClaimsPrincipal principal) =>
{
    SaveResult result = new SaveResult();
    input ??= new PasswordChangeInput();
    ValidationResults validationResults = input.Validate();

    string? id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
    string? method = principal.FindFirstValue(ClaimTypes.AuthenticationMethod);

    if (validationResults.IsValid)
    {
        if (!string.IsNullOrEmpty(id) && string.Equals(method, "Native", StringComparison.OrdinalIgnoreCase))
        {
            UserRecord? user = await context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
            if (user is not null)
            {
                if (user.VerifyPassword(input.OldPassword ?? string.Empty))
                {
                    user.SetPassword(input.NewPassword!);
                    await context.SaveChangesAsync();

                    result.Id = user.Id;
                    result.Success = true;
                }
                else
                {
                    validationResults.AddError(nameof(input.OldPassword), "Pôvodné heslo nie je správne");
                }
            }
            else
            {
                return Results.Unauthorized();
            }
        }
        else
        {
            return Results.Unauthorized();
        }
    }

    result.Errors = validationResults;

    return Results.Ok(result);
});

app.MapPost("/consume", async ([FromServices] Saml2Configuration saml2Configuration, [FromServices] ApplicationDbContext context, HttpRequest request, [FromServices] SigningCredentials signingCredentials, IConfiguration configuration, [FromServices] ILogger<Program> logger, [FromServices] TelemetryClient? telemetryClient) =>
{
    Saml2AuthnResponse saml2AuthnResponse = new Saml2AuthnResponse(saml2Configuration);

    Saml2PostBinding binding = new Saml2PostBinding();
    ITfoxtec.Identity.Saml2.Http.HttpRequest genericRequest = request.ToGenericHttpRequest();

    binding.ReadSamlResponse(genericRequest, saml2AuthnResponse);
    if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
    {
        return Results.BadRequest($"SAML Response status: {saml2AuthnResponse.Status}");
    }
    binding.Unbind(genericRequest, saml2AuthnResponse);
    logger.LogInformation(saml2AuthnResponse.XmlDocument.OuterXml);

    string? id = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.UPVSIdentityID")?.Value;
    string? firstName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.FirstName")?.Value;
    string? lastName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.LastName")?.Value;
    string? email = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.Email")?.Value;
    string? ico = saml2AuthnResponse.ClaimsIdentity.FindFirst("Subject.ICO")?.Value;
    string? companyName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Subject.FormattedName")?.Value;
    string? identificationNumber = saml2AuthnResponse.ClaimsIdentity.FindFirst("ActorID")?.Value;
    string? delegationType = saml2AuthnResponse.ClaimsIdentity.FindFirst("DelegationType")?.Value;
    string? formattedName = saml2AuthnResponse.ClaimsIdentity.FindFirst("Actor.FormattedName")?.Value;

    if (telemetryClient is not null)
    {
        foreach (Claim claim in saml2AuthnResponse.ClaimsIdentity.Claims)
        {
            telemetryClient.TrackTrace($"Claim: {claim.Type} = {claim.Value}");
        }
    }

    string? invitation = request.Cookies["invitation"];

    string? publisher = null;
    bool hasExplicitDelegation = true;
    if (!string.IsNullOrEmpty(ico) && delegationType == "0")
    {
        publisher = $"https://data.gov.sk/id/legal-subject/{ico}";
        hasExplicitDelegation = true;
    }

    List<Claim> customClaims = new List<Claim>();
    customClaims.AddRange(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type == Saml2ClaimTypes.NameId));
    customClaims.AddRange(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type == Saml2ClaimTypes.SessionIndex));
    customClaims.AddRange(saml2AuthnResponse.ClaimsIdentity.Claims.Where(c => c.Type == Saml2ClaimTypes.NameIdFormat));
   
    if (!string.IsNullOrEmpty(id))
    {
        UserRecord? user = await context.GetOrCreateUser(id, firstName, lastName, email, publisher, invitation, formattedName).ConfigureAwait(false);
        if (user is not null && user.IsActive)
        {
            return Results.Ok(await CreateToken(context, user, configuration, signingCredentials, hasExplicitDelegation, customClaims, companyName: companyName));
        }
        else
        {
            customClaims.Add(new Claim(ClaimTypes.NameIdentifier, id));
            customClaims.Add(new Claim(ClaimTypes.GivenName, firstName ?? string.Empty));
            customClaims.Add(new Claim(ClaimTypes.Surname, lastName ?? string.Empty));
            return Results.Ok(await CreateToken(context, null, configuration, signingCredentials, false, customClaims));
        }
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapPost("/harvester-login", (IConfiguration configuration, [FromServices] SigningCredentials signingCredentials, [FromBody] HarvesterAuthMessage? message) =>
{
    string? requiredAuth = configuration["HarvesterAuth"];
    if (!string.IsNullOrEmpty(requiredAuth) && string.Equals(requiredAuth, message?.Auth, StringComparison.Ordinal))
    {
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "Harvester"),
        };

        if (message?.PublisherId is not null)
        {
            claims.Add(new Claim("Publisher", message.PublisherId));
        }

        return Results.Ok(CreateJwtToken(configuration, signingCredentials, claims, DateTimeOffset.UtcNow.AddHours(6)));
    }
    else
    {
        return Results.Forbid();
    }
});

app.MapPost("/validate-invitation", async (IConfiguration configuration, [FromServices] ApplicationDbContext context, HttpRequest request, [FromServices] ILogger<Program> logger) =>
{
    CheckInvitationResult result = new CheckInvitationResult();
    string? invitation = request.Cookies["invitation"];
    if (!string.IsNullOrWhiteSpace(invitation))
    {
        UserRecord? user = await context.GetUserByInvitation(invitation);
        if (user is not null)
        {
            DateTimeOffset? expires = user.InvitedAt?.AddHours(48);

            result.FirstName = user.FirstName;
            result.LastName = user.LastName;
            result.ExpiresAt = expires;
            result.Publisher = user.Publisher;
            result.Role = user.Role;
            result.IsValid = expires.HasValue && DateTimeOffset.UtcNow <= expires.Value;
        }
    }
    return Results.Ok(result);
});

app.MapGet("/signin-google", async ([FromServices] ApplicationDbContext context, [FromServices] SigningCredentials signingCredentials, [FromServices] IConfiguration configuration, [FromServices] ILogger<Program> logger, [FromServices] IHttpClientFactory httpClientFactory, [FromQuery] string? code, [FromQuery] string? state) =>
{
    if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state) && string.Equals("google", state, StringComparison.Ordinal))
    {
        string? clientId = configuration["Authentication:Google:ClientId"];
        string? clientSecret = configuration["Authentication:Google:ClientSecret"];

        using HttpClientHandler handler = new HttpClientHandler();
        handler.SslProtocols = SslProtocols.Tls12;

        using (HttpClient client = new HttpClient(handler))
        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token"))
        {
            UriBuilder redirectUriBuilder = new UriBuilder(frontendUrl ?? string.Empty);
            redirectUriBuilder.Path = "/signin-google";

            request.Content = new FormUrlEncodedContent(new Dictionary<string, string?>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri" ,redirectUriBuilder.ToString() },
                { "grant_type", "authorization_code" }
            });

            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    GoogleTokenResponse? tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
                    if (tokenResponse?.AccessToken is not null)
                    {
                        Oauth2Service service = new Oauth2Service(new BaseClientService.Initializer()
                        {
                            HttpClientInitializer = GoogleCredential.FromAccessToken(tokenResponse.AccessToken),
                        });

                        Google.Apis.Oauth2.v2.Data.Userinfo userInfo = await service.Userinfo.Get().ExecuteAsync();

                        if (userInfo is not null)
                        {
                            logger.LogInformation(JsonConvert.SerializeObject(userInfo));

                            string? email = userInfo.Email;
                            string? id = userInfo.Id;
                            string? firstName = userInfo.GivenName;
                            string? lastName = userInfo.FamilyName;

                            List<Claim> customClaims = new List<Claim>
                            {
                                new Claim(ClaimTypes.AuthenticationMethod, "Google")
                            };

                            if (!string.IsNullOrEmpty(id))
                            {
                                UserRecord? user = await context.GetOrCreateExternalUser(id, firstName, lastName, email, "Google").ConfigureAwait(false);
                                if (user is not null && user.IsActive)
                                {
                                    return Results.Ok(await CreateToken(context, user, configuration, signingCredentials, false, customClaims));
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    return Results.Forbid();
});

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
    {
        IConfiguration configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        string? logPath = configuration["LogPath"];
        if (!string.IsNullOrEmpty(logPath) && Directory.Exists(logPath))
        {
            context.Request.EnableBuffering();
            string logName = $"{DateTimeOffset.UtcNow:yyyyMMddHHiiss.fffff}_{Guid.NewGuid():N}";
            string path = Path.Combine(logPath, logName);
            using (FileStream fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await context.Request.Body.CopyToAsync(fs);
            }
            context.Request.Body.Seek(0, SeekOrigin.Begin);

            RequestTelemetry? requestTelemetry = context.Features.Get<RequestTelemetry>();
            if (requestTelemetry is not null)
            {
                requestTelemetry.Properties["BodyLogFile"] = logName;
            }
        }
    }
    await next(context);
});

app.Run();

public partial class Program { }