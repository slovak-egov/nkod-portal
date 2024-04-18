using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Piranha;
using Piranha.AttributeBuilder;
using Piranha.Data.EF.SQLite;
using Piranha.Local;

var builder = WebApplication.CreateBuilder(args);

builder.AddPiranha(options =>
{
    options.UseCms();

    options.UseFileStorage(naming: FileStorageNaming.UniqueFolderNames);
    options.UseImageSharp();
    options.UseMemoryCache();

    var connectionString = builder.Configuration.GetConnectionString("piranha");
    options.UseEF<SQLiteDb>(db => db.UseSqlite(connectionString));
    
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


app.Run();