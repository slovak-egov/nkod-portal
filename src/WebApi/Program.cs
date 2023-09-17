using Microsoft.AspNetCore.Mvc;
using WebApi;
using DocumentStorageClient;
using NkodSk.Abstractions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing.Printing;
using System.IO;
using Abstractions;
using Newtonsoft.Json;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using IAMClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using VDS.RDF.Query.Algebra;
using Microsoft.OpenApi.Models;
using SaveResult = WebApi.SaveResult;
using System.Web;
using CodelistProviderClient;
using Lucene.Net.Search;
using VDS.RDF.Query.Expressions.Comparison;

var builder = WebApplication.CreateBuilder(args);

string? documentStorageUrl = builder.Configuration["DocumentStorageUrl"];
if (!Uri.IsWellFormedUriString(documentStorageUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get DocumentStorageUrl");
}

string? codelistProviderUrl = builder.Configuration["CodelistProviderUrl"];
if (!Uri.IsWellFormedUriString(codelistProviderUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get CodelistProviderUrl");
}

builder.Services.AddHttpClient(DocumentStorageClient.DocumentStorageClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(documentStorageUrl);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IDocumentStorageClient, DocumentStorageClient.DocumentStorageClient>();

builder.Services.AddHttpClient(CodelistProviderClient.CodelistProviderClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(codelistProviderUrl);
});
builder.Services.AddTransient<ICodelistProviderClient, CodelistProviderClient.CodelistProviderClient>();

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

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "LocalhostOrigin",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                      });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Frontend API",
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

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("LocalhostOrigin");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

FileStorageQuery MapQuery(AbstractQuery query, string language, bool allowAll = false)
{
    int page = query.Page ?? 1;
    int? pageSize = query.PageSize;
    if (!allowAll)
    {
        pageSize = query.PageSize ?? 10;

        if (page < 1)
        {
            throw new BadHttpRequestException("Page must be greater than 0");
        }

        if (pageSize < 1)
        {
            throw new BadHttpRequestException("PageSize must be greater than 0");
        }

        if (pageSize > 100)
        {
            throw new BadHttpRequestException("PageSize must be less than or equal to 100");
        }
    } 
    else if (pageSize < 0)
    {
        pageSize = null;
    }

    FileStorageOrderProperty? orderProperty = query.OrderBy?.ToLowerInvariant() switch
    {
        "name" => FileStorageOrderProperty.Name,
        "relevance" => FileStorageOrderProperty.Revelance,
        "created" => FileStorageOrderProperty.Created,
        "modified" => FileStorageOrderProperty.LastModified,
        _ => null
    };

    FileStorageQuery storageQuery = new FileStorageQuery
    {
        QueryText = query.QueryText,
        OnlyPublished = true,
        IncludeDependentFiles = true,
        SkipResults = pageSize.HasValue ? (page - 1) * pageSize.Value : 0,
        MaxResults = pageSize,
        OrderDefinitions = orderProperty.HasValue ? new List<FileStorageOrderDefinition> { new FileStorageOrderDefinition(orderProperty.Value, false) } : null,
        RequiredFacets = query.RequiredFacets,
        Language = language
    };

    if (query.Filters is not null)
    {
        Dictionary<string, string[]> filters = new Dictionary<string, string[]>(query.Filters, StringComparer.OrdinalIgnoreCase);
        if (filters.TryGetValue("publishers", out string[]? publishers))
        {
            storageQuery.OnlyPublishers = new List<string>(publishers);
            filters.Remove("publishers");
        }

        if (filters.TryGetValue("id", out string[]? ids))
        {
            if (ids.Length > 0)
            {
                List<Guid> values = new List<Guid>();
                foreach (string id in ids)
                {
                    if (Guid.TryParse(id, out Guid value))
                    {
                        values.Add(value);
                    }
                    else
                    {
                        throw new BadHttpRequestException($"Invalid value: {id}");
                    }
                }
                storageQuery.OnlyIds = values;
            }
            filters.Remove("id");
        }

        if (filters.TryGetValue("parent", out string[]? parents))
        {
            if (parents.Length > 0)
            {
                if (Guid.TryParse(parents[0], out Guid id))
                {
                    storageQuery.ParentFile = id;
                }
                else
                {
                    throw new BadHttpRequestException($"Invalid value: {parents[0]}");
                }
            }
            filters.Remove("parent");
        }

        storageQuery.AdditionalFilters = filters;
    }

    return storageQuery;
}

async Task<FileStorageResponse> GetStorageResponse(AbstractQuery query, string language, Func<FileStorageQuery, FileStorageQuery> storageQueryDecorator, IDocumentStorageClient client)
{
    FileStorageQuery? storageQuery = MapQuery(query, language);

    if (storageQuery.AdditionalFilters is not null)
    {
        if (storageQuery.AdditionalFilters.TryGetValue("sibling", out string[]? siblings))
        {
            if (siblings.Length > 0)
            {
                if (Guid.TryParse(siblings[0], out Guid id))
                {
                    FileMetadata? metadata = await client.GetFileMetadata(id).ConfigureAwait(false);
                    if (metadata is not null && metadata.ParentFile.HasValue)
                    {
                        storageQuery.ParentFile = metadata.ParentFile.Value;
                        storageQuery.ExcludeIds = new List<Guid> { id };
                    }
                    else
                    {
                        storageQuery = null;
                    }
                }
                else
                {
                    throw new BadHttpRequestException($"Invalid value: {siblings[0]}");
                }
            }

            if (storageQuery?.AdditionalFilters is not null)
            {
                storageQuery.AdditionalFilters.Remove("sibling");
            }
        }
    }

    if (storageQuery is not null)
    {
        storageQuery = storageQueryDecorator(storageQuery);

        return await client.GetFileStates(storageQuery).ConfigureAwait(false);
    }
    else
    {
        return new FileStorageResponse(new List<FileState>(), 0, new List<Facet>());
    }
}

async Task<Dictionary<string, PublisherView>> FetchPublishers(IDocumentStorageClient client, IEnumerable<string> keys, string language)
{
    Dictionary<string, PublisherView> publishers = new Dictionary<string, PublisherView>();

    FileStorageQuery storageQuery = new FileStorageQuery
    {
        OnlyPublished = true,
        OnlyPublishers = keys.ToList(),
        OnlyTypes = new List<FileType> { FileType.PublisherRegistration },
    };

    FileStorageResponse response = await client.GetFileStates(storageQuery).ConfigureAwait(false);
    if (response.Files.Count > 0)
    {
        FileState fileState = response.Files[0];
        if (fileState.Content is not null)
        {
            FoafAgent? agent = FoafAgent.Parse(fileState.Content);
            if (agent is not null)
            {
                PublisherView view = new PublisherView
                {
                    Id = fileState.Metadata.Id,
                    Name = agent.GetName(language),
                    Key = fileState.Metadata.Publisher
                };
                publishers[agent.Uri.ToString()] = view;
            }
        }
    }

    return publishers;
}

app.MapPost("/publishers/search", async ([FromBody] PublisherQuery query, [FromServices] IDocumentStorageClient client) => {
    string language = query.Language ?? "sk";

    FileStorageQuery groupQuery = MapQuery(query, language, true);

    FileStorageGroupResponse storageResponse = await client.GetFileStatesByPublisher(groupQuery).ConfigureAwait(false);

    AbstractResponse<PublisherView> response = new AbstractResponse<PublisherView>
    {
        TotalCount = storageResponse.TotalCount,
    };

    foreach (FileStorageGroup group in storageResponse.Groups)
    {
        if (group.PublisherFileState?.Content is not null)
        {
            FoafAgent? agent = FoafAgent.Parse(group.PublisherFileState.Content);
            if (agent is not null)
            {
                PublisherView view = new PublisherView
                {
                    Id = group.PublisherFileState.Metadata.Id,
                    Key = group.Key,
                    Name = agent.GetName(language),
                    DatasetCount = group?.Count ?? 0,
                    Themes = group?.Themes
                };
                response.Items.Add(view);
            }
        }
    }

    return response;
});

app.MapPost("/datasets/search", async ([FromBody] DatasetQuery query, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient) =>
{
    string language = query.Language ?? "sk";
    FileStorageResponse storageResponse = await GetStorageResponse(query, language, q =>
    {
        q.OnlyTypes = new List<FileType> { FileType.DatasetRegistration };
        q.IncludeDependentFiles = true;
        return q;
    }, client).ConfigureAwait(false);
    AbstractResponse<DatasetView> response = new AbstractResponse<DatasetView>
    {
        TotalCount = storageResponse.TotalCount,
        Facets = storageResponse.Facets
    };

    HashSet<string> publisherKeys = new HashSet<string>();

    foreach (FileState fileState in storageResponse.Files)
    {
        DcatDataset? datasetRdf = fileState.Content is not null ? DcatDataset.Parse(fileState.Content) : null;
        if (datasetRdf is not null)
        {
            DatasetView datasetView = await DatasetView.MapFromRdf(fileState.Metadata, datasetRdf, codelistProviderClient, language).ConfigureAwait(false);

            if (fileState.DependentFiles is not null)
            {
                foreach (FileState dependedState in fileState.DependentFiles)
                {
                    DcatDistribution? distributionRdf = dependedState.Content is not null ? DcatDistribution.Parse(dependedState.Content) : null;
                    if (distributionRdf is not null)
                    {
                        DistributionView distributionView = await DistributionView.MapFromRdf(dependedState.Metadata.Id, distributionRdf, codelistProviderClient, language);
                        datasetView.Distributions.Add(distributionView);
                    }
                }
            }

            response.Items.Add(datasetView);
            if (fileState.Metadata.Publisher is not null)
            {
                publisherKeys.Add(fileState.Metadata.Publisher);
            }
        }
    }

    Dictionary<string, PublisherView> publishers = await FetchPublishers(client, publisherKeys, language);
    foreach (DatasetView view in response.Items)
    {
        if (view.PublisherId is not null && publishers.TryGetValue(view.PublisherId, out PublisherView? publisher))
        {
            view.Publisher = publisher;
        }
    }

    return response;
});

app.MapPost("/local-catalogs/search", async ([FromBody] LocalCatalogsQuery query, [FromServices] IDocumentStorageClient client) =>
{
    string language = query.Language ?? "sk";
    FileStorageResponse storageResponse = await GetStorageResponse(query, language, q => {
        q.OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration };
        return q;
    }, client).ConfigureAwait(false);
    AbstractResponse<LocalCatalogView> response = new AbstractResponse<LocalCatalogView>
    {
        TotalCount = storageResponse.TotalCount,
        Facets = storageResponse.Facets
    };

    HashSet<string> publisherKeys = new HashSet<string>();

    foreach (FileState fileState in storageResponse.Files)
    {
        if (fileState.Content is not null)
        {
            DcatCatalog? catalogRdf = DcatCatalog.Parse(fileState.Content);
            if (catalogRdf is not null)
            {
                LocalCatalogView view = await LocalCatalogView.MapFromRdf(fileState.Metadata, catalogRdf, language).ConfigureAwait(false);

                response.Items.Add(view);
                if (fileState.Metadata.Publisher is not null)
                {
                    publisherKeys.Add(fileState.Metadata.Publisher);
                }
            }
        }
    }

    Dictionary<string, PublisherView> publishers = await FetchPublishers(client, publisherKeys, language);
    foreach (LocalCatalogView view in response.Items)
    {
        if (view.PublisherId is not null && publishers.TryGetValue(view.PublisherId, out PublisherView? publisher))
        {
            view.Publisher = publisher;
        }
    }

    return response;
});

app.MapGet("/codelists", async ([FromQuery(Name = "keys[]")] string[] keys, [FromServices] ICodelistProviderClient codelistProviderClient) =>
{
    string language = "sk";
    List<CodelistView> codelists = new List<CodelistView>();
    foreach (string key in keys)
    {
        Codelist? codelist = await codelistProviderClient.GetCodelist(key).ConfigureAwait(false);
        if (codelist is not null)
        {
            List<CodelistItemView> values = new List<CodelistItemView>(codelist.Items.Count);
            foreach (CodelistItem item in codelist.Items.Values)
            {
                values.Add(new CodelistItemView(item.Id, item.GetCodelistValueLabel(language)));
            }
            codelists.Add(new CodelistView(codelist.Id, codelist.GetLabel(language), values));
        }
    }
    return codelists;
});

app.MapPost("/datasets", [Authorize] async ([FromBody] DatasetInput? dataset, [FromServices] IDocumentStorageClient client, ClaimsPrincipal user) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher))
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatDataset? datasetRdf = dataset?.MapToRdf(publisher, out errors);
        if (datasetRdf is not null && dataset is not null)
        {         
            FileMetadata metadata = datasetRdf.UpdateMetadata(dataset.IsPublic);
            await client.InsertFile(datasetRdf.ToString(), false, metadata).ConfigureAwait(false);
            result.Id = metadata.Id.ToString();
            result.Success = true;
            result.Errors = errors;
        }
    } 
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapPut("/datasets", [Authorize] async ([FromBody] DatasetInput dataset, [FromServices] IDocumentStorageClient client) =>
{
    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatDataset? datasetRdf = dataset?.MapToRdf(new Uri(""), out errors);
        if (datasetRdf is not null && dataset is not null && Guid.TryParse(dataset.Id, out Guid id))
        {
            FileMetadata? metadata = await client.GetFileMetadata(id).ConfigureAwait(false);
            if (metadata is not null)
            {
                metadata = datasetRdf.UpdateMetadata(dataset.IsPublic, metadata);
                await client.InsertFile(datasetRdf.ToString(), true, metadata).ConfigureAwait(false);
                result.Id = metadata.Id.ToString();
                result.Success = true;
                result.Errors = errors;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "File not found";
        }
    }
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapDelete("/datasets", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client) =>
{
    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            await client.DeleteFile(key).ConfigureAwait(false);
            return Results.Ok();
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

app.MapPost("/distributions", [Authorize] async ([FromBody] DistributionInput distribution, [FromServices] IDocumentStorageClient client) =>
{
    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatDistribution? rdf = distribution?.MapToRdf(out errors);
        if (rdf is not null && distribution is not null)
        {
            FileMetadata metadata = rdf.UpdateMetadata(null!);
            await client.InsertFile(rdf.ToString(), false, metadata).ConfigureAwait(false);
            result.Id = metadata.Id.ToString();
            result.Success = true;
            result.Errors = errors;
        }
    }
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapPut("/distributions", [Authorize] async ([FromBody] DistributionInput distribution, [FromServices] IDocumentStorageClient client) =>
{
    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatDistribution? rdf = distribution?.MapToRdf(out errors);
        if (rdf is not null && distribution is not null && Guid.TryParse(distribution.Id, out Guid id))
        {
            FileMetadata? metadata = await client.GetFileMetadata(id).ConfigureAwait(false);
            if (metadata is not null)
            {
                metadata = rdf.UpdateMetadata(metadata);
                await client.InsertFile(rdf.ToString(), true, metadata).ConfigureAwait(false);
                result.Id = metadata.Id.ToString();
                result.Success = true;
                result.Errors = errors;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "File not found";
        }
    }
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapDelete("/distributions", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client) =>
{
    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            await client.DeleteFile(key).ConfigureAwait(false);
            return Results.Ok();
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

app.MapPost("/local-catalogs", [Authorize] async ([FromBody] LocalCatalogInput catalog, [FromServices] IDocumentStorageClient client) =>
{
    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatCatalog? rdf = catalog?.MapToRdf(out errors);
        if (rdf is not null && catalog is not null)
        {
            FileMetadata metadata = rdf.UpdateMetadata();
            await client.InsertFile(rdf.ToString(), false, metadata).ConfigureAwait(false);
            result.Id = metadata.Id.ToString();
            result.Success = true;
            result.Errors = errors;
        }
    }
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapPut("/local-catalogs", [Authorize] async ([FromBody] LocalCatalogInput catalog, [FromServices] IDocumentStorageClient client) =>
{
    SaveResult result = new SaveResult();
    try
    {
        Dictionary<string, string>? errors = null;
        DcatCatalog? rdf = catalog?.MapToRdf(out errors);
        if (rdf is not null && catalog is not null && Guid.TryParse(catalog.Id, out Guid id))
        {
            FileMetadata? metadata = await client.GetFileMetadata(id).ConfigureAwait(false);
            if (metadata is not null)
            {
                metadata = rdf.UpdateMetadata(metadata);
                await client.InsertFile(rdf.ToString(), true, metadata).ConfigureAwait(false);
                result.Id = metadata.Id.ToString();
                result.Success = true;
                result.Errors = errors;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "File not found";
        }
    }
    catch (Exception)
    {
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return Results.Ok(result);
});

app.MapDelete("/local-catalogs", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client) =>
{
    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            await client.DeleteFile(key).ConfigureAwait(false);
            return Results.Ok();
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

app.MapPut("/publishers", [Authorize] async ([FromBody] PublisherInput input, [FromServices] IDocumentStorageClient client) =>
{
    return new SaveResult();
});

app.MapDelete("publishers", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client) =>
{
    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            await client.DeleteFile(key).ConfigureAwait(false);
            return Results.Ok();
        }
        else
        {
            return Results.NotFound();
        }
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }
});

app.MapPost("user-info", [Authorize] async ([FromServices] IIdentityAccessManagementClient client) =>
{
    return new UserInfo();
});

app.MapPost("login", [Authorize] async ([FromServices] IIdentityAccessManagementClient client) =>
{
    return new DelegationAuthorizationResult();
});

app.MapPost("logout", [Authorize] async ([FromServices] IIdentityAccessManagementClient client) =>
{
    return new DelegationAuthorizationResult();
});

app.MapPost("publishers/impersonate", [Authorize] async ([FromQuery] string? id, [FromServices] IIdentityAccessManagementClient client) =>
{
    return new TokenResult();
});

app.MapPut("/codelists", [Authorize] async ([FromServices] IDocumentStorageClient client, IFormFile file) =>
{
    return Results.Ok();
});

app.MapGet("/users", [Authorize] async ([FromServices] IIdentityAccessManagementClient client) =>
{
    return new List<UserInfo>();
});

app.MapPost("/users", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, NewUserInput input) =>
{
    return new SaveResult();
});

app.MapPut("/users", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, EditUserInput input) =>
{
    return new SaveResult();
});

app.MapDelete("/users", [Authorize] async ([FromQuery] string? id, [FromServices] IIdentityAccessManagementClient client) =>
{
    return Results.Ok();
});

app.MapPost("/registration", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, RegistrationInput input) =>
{
    return new RegistrationResult();
});

app.MapPost("/upload", [Authorize] async ([FromServices] IDocumentStorageClient client, ClaimsPrincipal identity, HttpRequest request, IFormFile file) =>
{
    if (file is not null)
    {
        string? publisher = identity.FindFirstValue("Publisher");
        if (!string.IsNullOrEmpty(publisher))
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            FileMetadata metadata = new FileMetadata(Guid.NewGuid(), file.FileName, FileType.DistributionFile, null, publisher, true, file.FileName, now, now);
            using Stream stream = file.OpenReadStream();
            await client.UploadStream(stream, metadata, false).ConfigureAwait(false);
            return Results.Ok(new FileUploadResult
            {
                Id = metadata.Id.ToString(),
                Url = $"{request.Scheme}://{request.Host}/download?id={HttpUtility.HtmlEncode(metadata.Id)}"
            });
        }
        else
        {
            return Results.Forbid();
        }
    }
    else
    {
        return Results.BadRequest();
    }
}).Produces<FileUploadResult>();

app.MapGet("/download", async ([FromServices] IDocumentStorageClient client, [FromQuery] string? id) =>
{
    string language = "sk";
    if (Guid.TryParse(id, out Guid key))
    {
        FileMetadata? metadata = await client.GetFileMetadata(key).ConfigureAwait(false);
        if (metadata is not null)
        {
            if (metadata.Type == FileType.DistributionFile)
            {
                Stream? stream = await client.DownloadStream(key).ConfigureAwait(false);
                if (stream is not null)
                {
                    FileStreamHttpResult r = (FileStreamHttpResult)Results.File(stream, fileDownloadName: metadata.OriginalFileName ?? metadata.Name.GetText(language) ?? metadata.Id.ToString());

                    return r;
                }
            }
        }
    }
    return Results.NotFound();
});

app.MapGet("/quality", async ([FromServices] IDocumentStorageClient client) =>
{
    return new QualityResult();
});

app.UseSpa(config =>
{
    
});

app.Run();

public partial class Program { }