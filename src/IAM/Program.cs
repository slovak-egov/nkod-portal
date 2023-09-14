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
using Azure.Core;
using System.Security.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using IAMClient;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddHttpClient();

builder.Services.AddSingleton(services =>
{
    Saml2Configuration saml2Configuration = new Saml2Configuration();
    builder.Configuration.Bind("Saml2", saml2Configuration);

    saml2Configuration.SigningCertificate = CertificateUtil.Load(builder.Configuration["Saml2:SigningCertificateFile"], builder.Configuration["Saml2:SigningCertificatePassword"], X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
    saml2Configuration.AllowedAudienceUris.Add(saml2Configuration.Issuer);

    var httpClientFactory = services.GetService<IHttpClientFactory>();
    var entityDescriptor = new EntityDescriptor();
    entityDescriptor.ReadIdPSsoDescriptorFromUrlAsync(httpClientFactory, new Uri(builder.Configuration["Saml2:IdPMetadata"])).GetAwaiter().GetResult();
    if (entityDescriptor.IdPSsoDescriptor != null)
    {
        saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
        saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
        saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;
        foreach (var signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
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

app.UseSaml2();

app.MapGet("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, int? limit, int? offset) =>
{
    return new List<UserInfo>();
});

app.MapPost("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromBody] NewUserInput? input) =>
{
    return new SaveResult();
});

app.MapPut("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromBody] EditUserInput? input) =>
{
    return new SaveResult();
});

app.MapDelete("/users", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user, [FromQuery] string? id) =>
{
    return Results.Ok();
});

app.MapGet("/user-info", [Authorize] async ([FromServices] ApplicationDbContext context, ClaimsPrincipal user) =>
{
    return new UserInfo();
});

app.MapGet("/login", async ([FromServices] Saml2Configuration saml2Configuration) =>
{
    
});

app.MapPost("/consume", ([FromServices] Saml2Configuration saml2Configuration, IHttpContextAccessor httpContextAccessor) =>
{
    
});

app.MapGet("/logout", [Authorize] async ([FromServices] ApplicationDbContext context, [FromServices] ClaimsPrincipal user) =>
{
    return new DelegationAuthorizationResult();
});

app.MapGet("/delegate-publisher", [Authorize] async ([FromServices] ApplicationDbContext context, [FromServices] ClaimsPrincipal user, [FromQuery] string? publisher) =>
{
    return new TokenResult();
});

app.Run();