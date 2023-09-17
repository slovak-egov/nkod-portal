using Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NkodSk.Abstractions;
using System.Web;

var builder = WebApplication.CreateBuilder(args);

string? documentStorageUrl = builder.Configuration["DocumentStorageUrl"];
if (!Uri.IsWellFormedUriString(documentStorageUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get DocumentStorageUrl");
}

builder.Services.AddHttpClient(DocumentStorageClient.DocumentStorageClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(documentStorageUrl);
});
builder.Services.AddSingleton<IDocumentStorageClient, DocumentStorageClient.DocumentStorageClient>();
builder.Services.AddSingleton<InternalCodelistProvider>();
builder.Services.AddSingleton<ILanguagesSource, DefaultLanguagesSource>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CodelistProvider API",
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

app.MapGet("/codelists", async (InternalCodelistProvider provider) =>
{
    return await provider.GetCodelists();
});

app.MapGet("/codelists/{id}", async (InternalCodelistProvider provider, string? id) =>
{
    if (id is not null)
    {
        id = HttpUtility.UrlDecode(id);
    }
    Codelist? list = await provider.GetCodelist(id);
    if (list is not null)
    {
        return Results.Ok(list);
    }
    else
    {
        return Results.NotFound();
    }
});


app.MapPut("/codelists", async (InternalCodelistProvider provider, [FromServices] IDocumentStorageClient client, string? id) =>
{
    return Results.Ok();
});


app.Run();

public partial class Program { }