using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NkodSk.Abstractions;
using System.Security.Claims;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

string? fileStroragePath = builder.Configuration["StoragePath"];

if (!Directory.Exists(fileStroragePath))
{
    throw new Exception("File storage does not exist");
}

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
).AddJwtBearer(o =>
{
    RSA rsa = RSA.Create();
    string? publicKeyString = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(publicKeyString))
    {
        throw new Exception("Unable to get Jwt:Key");
    }

    try
    {
        rsa.ImportFromPem(publicKeyString);
    }
    catch (Exception e)
    {
        throw new Exception("Unable to decode Jwt:Key", e);
    }

    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(false)),
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();

string? GetSettingsFilePath(string? name)
{
    if (string.IsNullOrEmpty(name))
    {
        return null;
    }

    foreach (char c in Path.GetInvalidFileNameChars())
    {
        name = name.Replace(c, '_');
    }

    string path = Path.GetFullPath(Path.Combine(fileStroragePath, name));
    if (path.StartsWith(fileStroragePath))
    {
        return path;
    }

    return null;
}

app.MapGet("/settings", ([FromQuery] string? name, ClaimsPrincipal? principal) =>
{
    if (principal?.Identity?.IsAuthenticated ?? false)
    {
        bool isSuperAdmin = principal.IsInRole("Superadmin");
        bool isPublisher = principal.IsInRole("Publisher");
        bool isPublisherAdmin = principal.IsInRole("PublisherAdmin");

        if (string.IsNullOrEmpty(name) && isSuperAdmin)
        {
            name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        }

        string? path = GetSettingsFilePath(name);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            MonitoringNotificationSettings? setting = System.Text.Json.JsonSerializer.Deserialize<MonitoringNotificationSettings>(File.ReadAllText(path));
            return Results.Ok(setting);
        }
    }
    return Results.NotFound();
});

app.MapPut("/settings", async ([FromBody] MonitoringNotificationSettings? settings, ClaimsPrincipal? principal) =>
{
    if (principal?.Identity?.IsAuthenticated ?? false)
    {
        bool isSuperAdmin = principal.IsInRole("Superadmin");
        bool isPublisher = principal.IsInRole("Publisher");
        bool isPublisherAdmin = principal.IsInRole("PublisherAdmin");

        string? name = null;

        if (isSuperAdmin)
        {
            if (!string.IsNullOrEmpty(settings?.UseForLocalCatalog))
            {
                name = settings.UseForLocalCatalog;
            }
            else
            {
                name = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            }
        }
        else if (isPublisher || isPublisherAdmin)
        {
            if (!string.IsNullOrEmpty(settings?.UseForLocalCatalog))
            {
                name = settings.UseForLocalCatalog;
            }
        }

        string? path = GetSettingsFilePath(name);
        if (!string.IsNullOrEmpty(path))
        {
            if (settings is not null)
            {
                File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings));
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
            return Results.Ok();
        }
        else
        {
            return Results.BadRequest("Invalid file");
        }
    }

    return Results.Forbid();
});

app.MapGet("/all", (ClaimsPrincipal? principal) =>
{
    if (principal?.IsInRole("Harvester") ?? false)
    {
        List<MonitoringNotificationSettings> list = [];

        foreach (string path in Directory.GetFiles(fileStroragePath, "*", SearchOption.TopDirectoryOnly))
        {
            MonitoringNotificationSettings? setting = System.Text.Json.JsonSerializer.Deserialize<MonitoringNotificationSettings>(File.ReadAllText(path));
            if (setting is not null)
            {
                list.Add(setting);
            }
        }

        return Results.Ok(list);
    }
    return Results.Forbid();
});

app.Run();
