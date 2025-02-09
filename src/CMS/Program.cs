using CMS;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Piranha;
using Piranha.AttributeBuilder;
using Piranha.Data.EF.SQLite;
using Piranha.Local;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Cryptography;
using Microsoft.ApplicationInsights.Extensibility;
using NkodSk.Abstractions;

var builder = WebApplication.CreateBuilder(args);

string documentStorageUrl = builder.Configuration["DocumentStorageUrl"];
if (Uri.IsWellFormedUriString(documentStorageUrl, UriKind.Absolute))
{
    builder.Services.AddHttpClient(DocumentStorageClient.DocumentStorageClient.HttpClientName, c =>
    {
        c.BaseAddress = new Uri(documentStorageUrl);
    });
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddTransient<IHttpContextValueAccessor, EmptyHttpContextValueAccessor>();
    builder.Services.AddTransient<IDocumentStorageClient, DocumentStorageClient.DocumentStorageClient>();
}

builder.AddPiranha(options =>
{
    options.UseCms();

    options.UseFileStorage(naming: FileStorageNaming.UniqueFolderNames);
    options.UseImageSharp();
    options.UseMemoryCache();

    var connectionString = builder.Configuration.GetConnectionString("piranha");
    options.UseEF<SQLiteDb>(db => db.UseSqlite(connectionString));
		
	options.Services.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
	}).AddJwtBearer(options =>
	{
        RSA rsa = RSA.Create();
        string publicKeyString = builder.Configuration["Authentication:SecretForKey"];
        rsa.ImportFromPem(publicKeyString);
        if (string.IsNullOrEmpty(publicKeyString))
        {
            throw new Exception("Unable to get Authentication:SecretForKey");
        }

		var key = new RsaSecurityKey(rsa);

		options.RequireHttpsMetadata = false;
		options.SaveToken = true;
		options.TokenValidationParameters = new()
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = builder.Configuration["Authentication:Issuer"],
			ValidAudience = builder.Configuration["Authentication:Audience"],			
			IssuerSigningKey = key
		};
	});
	options.Services.AddAuthorization();

	options.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "CMS API", Version = "v1" });
        options.CustomSchemaIds(x => x.FullName);
		options.SchemaFilter<EnumSchemaFilter>();
		options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
		{
			In = ParameterLocation.Header,
			Description = "Please enter a valid token",
			Name = "Authorization",
			Type = SecuritySchemeType.Http,
			BearerFormat = "JWT",
			Scheme = "Bearer"
		});
		options.AddSecurityRequirement(new OpenApiSecurityRequirement
		{
			{
				new OpenApiSecurityScheme
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				},
				Array.Empty<string>()
			}
		});
	});

	options.Services.AddControllers().AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});
});

builder.Services.AddApplicationInsightsTelemetry(options =>
{
	options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
	options.EnableAdaptiveSampling = false;
});
builder.Services.AddSingleton<ITelemetryInitializer, RequestTelementryInitializer>();

builder.Services.AddSingleton<INotificationService>(sp => new NotificationService(builder.Configuration["NotificationService"], builder.Configuration["FrontendUrl"]));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UsePathBase("/cms");

app.UsePiranha(options =>
{
    // Initialize Piranha
    App.Init(options.Api);

    // Build content types
    new ContentTypeBuilder(options.Api)
        .AddAssembly(typeof(Program).Assembly)
        .Build()
        .DeleteOrphans();
    
    app.Use(async (ctx, next) =>
    {
        await next();

        if (ctx.Response.StatusCode == 401)
        {
            await ctx.Response.WriteAsync("failed");
        }
    });

    app.MapControllers();

    app.UseCors(x => x
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(_ => app.Environment.IsDevelopment())
        .AllowCredentials());
});

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/cms/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});

app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program { }