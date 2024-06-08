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

var builder = WebApplication.CreateBuilder(args);

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
		var publicKeyBytes = Convert.FromBase64String(builder.Configuration["Authentication:SecretForKey"]);
		var rsa = RSA.Create(2048);
		rsa.ImportRSAPublicKey(publicKeyBytes, out _);
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