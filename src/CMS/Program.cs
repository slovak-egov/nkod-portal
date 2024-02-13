using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Piranha;
using Piranha.AspNetCore.Identity;
using Piranha.AspNetCore.Identity.SQLite;
using Piranha.AttributeBuilder;
using Piranha.Data.EF.SQLite;
using Piranha.Local;
using Piranha.Manager.Editor;

var builder = WebApplication.CreateBuilder(args);

builder.AddPiranha(options =>
{
    options.UseCms();
    if (builder.Environment.IsDevelopment())
    {
        options.Services.Configure<MvcOptions>(o => o.Filters.Add(typeof(IgnoreAntiforgeryTokenAttribute), int.MaxValue));
    }

    options.UseManager();

    options.UseFileStorage(naming: FileStorageNaming.UniqueFolderNames);
    options.UseImageSharp();
    options.UseTinyMCE();
    options.UseMemoryCache();

    var connectionString = builder.Configuration.GetConnectionString("piranha");
    options.UseEF<SQLiteDb>(db => db.UseSqlite(connectionString));
    options.UseIdentityWithSeed<IdentitySQLiteDb>(db => db.UseSqlite(connectionString));
    
    options.Services.AddAuthorization(o => 
        o.AddPolicy(Permissions.UsersSave, policy =>
    {
        policy.Requirements.Clear();
        policy.RequireAssertion(_ => true);
    }));
    
    options.UseSecurity(b =>
    {
        b.UsePermission("PortalUser", "Portal User");
    }, o =>
    {
        o.LoginUrl = "/odkomunita/login";
    });

	options.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "CMS API", Version = "v1" });
        options.CustomSchemaIds(x => x.FullName);
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

    // Configure Tiny MCE
    EditorConfig.FromFile("editorconfig.json");

    options.UseTinyMCE();
    options.UseIdentity();
    
    app.UseCors(x => x
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed(_ => app.Environment.IsDevelopment())
        .AllowCredentials());

    options.UseManager();
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/cms/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = string.Empty;
});


app.Run();