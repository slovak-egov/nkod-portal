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
using System.Web;
using CodelistProviderClient;
using Lucene.Net.Search;
using VDS.RDF.Query.Expressions.Comparison;
using System.Security.Policy;
using System.Data;
using AngleSharp.Io;
using System.Reflection.Metadata;
using UserInfo = NkodSk.Abstractions.UserInfo;
using Newtonsoft.Json.Serialization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using System.Net.Mime;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

string? iamClientUrl = builder.Configuration["IAMUrl"];
if (!Uri.IsWellFormedUriString(iamClientUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get IAMUrl");
}

builder.Services.AddHeaderPropagation(options =>
{
    options.Headers.Add("Cookie");
});

builder.Services.AddHttpClient(DocumentStorageClient.DocumentStorageClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(documentStorageUrl);
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IHttpContextValueAccessor, HttpContextValueAccessor>();
builder.Services.AddTransient<IDocumentStorageClient, DocumentStorageClient.DocumentStorageClient>();

builder.Services.AddHttpClient(CodelistProviderClient.CodelistProviderClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(codelistProviderUrl);
});
builder.Services.AddTransient<ICodelistProviderClient, CodelistProviderClient.CodelistProviderClient>();

builder.Services.AddHttpClient(IdentityAccessManagementClient.HttpClientName, c =>
{
    c.BaseAddress = new Uri(iamClientUrl);
}).AddHeaderPropagation();
builder.Services.AddTransient<IIdentityAccessManagementClient, IdentityAccessManagementClient>();

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

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = new[] { "sk-Sk" };
    options.SetDefaultCulture(supportedCultures[0]);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "LocalhostOrigin",
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                      });

    options.AddPolicy(name: "eFormulare",
                     policy =>
                     {
                         policy.WithOrigins(new[] {
                         "https://app.eformulare.sk",
                         "https://www.slovensko.sk",
                         "https://portal.upvsfixnew.gov.sk",
                         "https://schranka.slovensko.sk",
                         "https://schranka.upvsfixnew.gov.sk",
                         "http://localhost:3000",
                         }).AllowAnyHeader().AllowAnyMethod();
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

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
    options.EnableAdaptiveSampling = false;    
}).AddApplicationInsightsTelemetryProcessor<ExceptionFilter>();
builder.Services.AddSingleton<ITelemetryInitializer, RequestTelementryInitializer>();

const int maxFileSize = 250 * 1024 * 1024;

builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = maxFileSize;
    options.MultipartBodyLengthLimit = maxFileSize + 10 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxFileSize + 10 * 1024 * 1024;
});

ImportHarvestedHostedService importHarvestedHostedService = new ImportHarvestedHostedService(documentStorageUrl, iamClientUrl, builder.Configuration["HarvesterAuthToken"] ?? string.Empty, builder.Configuration["PrivateSparqlEndpoint"] ?? string.Empty);
builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService>(importHarvestedHostedService));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseHeaderPropagation();
app.UseRequestLocalization();

app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "text/plain"
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
    app.UseCors("LocalhostOrigin");
}
else
{
    app.UseSwagger();
    app.UseCors("eFormulare");
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

        if (pageSize > 10000)
        {
            throw new BadHttpRequestException("PageSize must be less than or equal to 100");
        }
    } 
    else if (pageSize < 0)
    {
        pageSize = null;
    }

    FileStorageOrderDefinition? orderDefinition = query.OrderBy?.ToLowerInvariant() switch
    {
        "name" => new FileStorageOrderDefinition(FileStorageOrderProperty.Name, false),
        "relevance" => new FileStorageOrderDefinition(FileStorageOrderProperty.Relevance, false),
        "created" => new FileStorageOrderDefinition(FileStorageOrderProperty.Created, true),
        "modified" => new FileStorageOrderDefinition(FileStorageOrderProperty.LastModified, true),
        _ => null
    };

    FileStorageQuery storageQuery = new FileStorageQuery
    {
        QueryText = query.QueryText,
        SkipResults = pageSize.HasValue ? (page - 1) * pageSize.Value : 0,
        MaxResults = pageSize,
        OrderDefinitions = orderDefinition.HasValue ? new List<FileStorageOrderDefinition> { orderDefinition.Value } : null,
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

async Task<FileStorageResponse> GetStorageResponse(AbstractQuery query, string language, Func<FileStorageQuery, FileStorageQuery> storageQueryDecorator, IDocumentStorageClient client, bool allowAll = false)
{
    FileStorageQuery? storageQuery = MapQuery(query, language, allowAll);

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
        foreach (FileState fileState in response.Files)
        {
            if (fileState.Content is not null)
            {
                FoafAgent? agent = FoafAgent.Parse(fileState.Content);
                if (agent is not null)
                {
                    publishers[agent.Uri.ToString()] = PublisherView.MapFromRdf(fileState.Metadata.Id, fileState.Metadata.IsPublic, 0, agent, null, language, false);
                }
            }
        }
    }

    return publishers;
}

app.MapPost("/publishers/search", async ([FromBody] PublisherQuery query, [FromServices] IDocumentStorageClient client, [FromServices] TelemetryClient? telemetryClient) => {
    try
    {
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
                    response.Items.Add(PublisherView.MapFromRdf(group.PublisherFileState.Metadata.Id, group.PublisherFileState.Metadata.IsPublic, group.Count, agent, group.Themes, language, true));
                }
            }
        }

        return Results.Ok(response);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<PublisherView>>();

app.MapPost("/datasets/search", async ([FromBody] DatasetQuery query, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal? user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        bool isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        string language = query.Language ?? "sk";

        FileStorageResponse storageResponse;

        try
        {
            storageResponse = await GetStorageResponse(query, language, q =>
            {
                q.OnlyTypes = new List<FileType> { FileType.DatasetRegistration };
                q.IncludeDependentFiles = true;
                return q;
            }, client, isAuthenticated).ConfigureAwait(false);
        } 
        catch (BadHttpRequestException)
        {
            storageResponse = new FileStorageResponse(new List<FileState>(), 0, new List<Facet>());
        }

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
                DatasetView datasetView = await DatasetView.MapFromRdf(fileState.Metadata, datasetRdf, codelistProviderClient, language, isAuthenticated).ConfigureAwait(false);

                if (fileState.DependentFiles is not null)
                {
                    foreach (FileState dependedState in fileState.DependentFiles.Where(f => f.Metadata.Type == FileType.DistributionRegistration))
                    {
                        DcatDistribution? distributionRdf = dependedState.Content is not null ? DcatDistribution.Parse(dependedState.Content) : null;
                        if (distributionRdf is not null)
                        {
                            DistributionView distributionView = await DistributionView.MapFromRdf(dependedState.Metadata.Id, fileState.Metadata.Id, distributionRdf, codelistProviderClient, language, isAuthenticated);
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

        return Results.Ok(response);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<DatasetView>>();

app.MapPost("/distributions/search", async ([FromBody] DatasetQuery query, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal? user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        bool isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        string language = query.Language ?? "sk";
        FileStorageResponse storageResponse = await GetStorageResponse(query, language, q =>
        {
            q.OnlyTypes = new List<FileType> { FileType.DistributionRegistration };
            return q;
        }, client, isAuthenticated).ConfigureAwait(false);
        AbstractResponse<DistributionView> response = new AbstractResponse<DistributionView>
        {
            TotalCount = storageResponse.TotalCount,
            Facets = storageResponse.Facets
        };

        foreach (FileState fileState in storageResponse.Files)
        {
            DcatDistribution? distributionRdf = fileState.Content is not null ? DcatDistribution.Parse(fileState.Content) : null;
            if (distributionRdf is not null)
            {
                DistributionView view = await DistributionView.MapFromRdf(fileState.Metadata.Id, fileState.Metadata.ParentFile, distributionRdf, codelistProviderClient, language, isAuthenticated).ConfigureAwait(false);

                response.Items.Add(view);
            }
        }

        return Results.Ok(response);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<DistributionView>>();

app.MapPost("/local-catalogs/search", async ([FromBody] LocalCatalogsQuery query, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal? user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        bool isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        string language = query.Language ?? "sk";
        FileStorageResponse storageResponse = await GetStorageResponse(query, language, q => {
            q.OnlyTypes = new List<FileType> { FileType.LocalCatalogRegistration };
            return q;
        }, client, isAuthenticated).ConfigureAwait(false);
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
                    LocalCatalogView view = await LocalCatalogView.MapFromRdf(fileState.Metadata, catalogRdf, codelistProviderClient, language, isAuthenticated);

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

        return Results.Ok(response);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<LocalCatalogView>>();

app.MapGet("/codelists", async ([FromQuery(Name = "keys[]")] string[] keys, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        string language = "sk";
        List<CodelistView> codelists = new List<CodelistView>();
        foreach (string key in keys)
        {
            if (Uri.IsWellFormedUriString(key, UriKind.Absolute))
            {
                Codelist? codelist = await codelistProviderClient.GetCodelist(key).ConfigureAwait(false);
                if (codelist is not null)
                {
                    List<CodelistItemView> values = new List<CodelistItemView>(codelist.Items.Count);
                    foreach (CodelistItem item in codelist.Items.Values)
                    {
                        values.Add(new CodelistItemView(item.Id, item.GetCodelistValueLabel(language)));
                    }
                    values.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label));
                    codelists.Add(new CodelistView(codelist.Id, codelist.GetLabel(language), values));
                }
            }
        }
        return Results.Ok(codelists);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<CodelistView[]>>();

app.MapGet("/codelists/item", async (string key, string id, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        string language = "sk";
        CodelistItem? item = await codelistProviderClient.GetCodelistItem(key, id).ConfigureAwait(false);
        if (item is not null)
        {
            return Results.Ok(new CodelistItemView(item.Id, item.GetCodelistValueLabel(language)));
        }
        return Results.NotFound();
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<AbstractResponse<CodelistItemView>>();

app.MapPost("/codelists/item", async ([FromQuery] string key, [FromQuery] string query, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        string language = "sk";
        List<CodelistView> codelists = new List<CodelistView>();
        Codelist? codelist = await codelistProviderClient.GetCodelist(key).ConfigureAwait(false);
        if (codelist is not null)
        {
            List<CodelistItemView> values = new List<CodelistItemView>(codelist.Items.Count);
            foreach (CodelistItem item in codelist.Items.Values)
            {
                string label = item.GetCodelistValueLabel(language);
                if (label.Contains(query))
                {
                    values.Add(new CodelistItemView(item.Id, label));
                }
            }
            values.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label));
            codelists.Add(new CodelistView(codelist.Id, codelist.GetLabel(language), values));
        }
        return Results.Ok(codelists);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/datasets", [Authorize] async ([FromBody] DatasetInput? dataset, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (dataset is not null)
        {
            ValidationResults validationResults = await dataset.Validate(publisherId, client, codelistProviderClient);
            if (validationResults.IsValid)
            {
                Guid? parentDataset = null;
                if (!string.IsNullOrEmpty(dataset.IsPartOf) && Guid.TryParse(dataset.IsPartOf, out Guid parentDatasetId))
                {
                    parentDataset = parentDatasetId;
                }

                DcatDataset datasetRdf = DcatDataset.Create();
                dataset.MapToRdf(publisher, datasetRdf);
                FileMetadata metadata = datasetRdf.UpdateMetadata(dataset.IsSerie);
                metadata = await datasetRdf.UpdateReferenceToParent(parentDataset, metadata, client);
                await client.InsertFile(datasetRdf.ToString(), false, metadata).ConfigureAwait(false);
                result.Id = metadata.Id.ToString();
                result.Success = true;
            }
            else
            {
                result.Errors = validationResults;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPut("/datasets", [Authorize] async ([FromBody] DatasetInput? dataset, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
        if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
        {
            return Results.Forbid();
        }

        FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
        if (publisherState is null || !publisherState.Metadata.IsPublic)
        {
            return Results.Forbid();
        }

        SaveResult result = new SaveResult();
        try
        {
            if (dataset is not null)
            {
                if (Guid.TryParse(dataset.Id, out Guid id))
                {
                    FileState? state = await client.GetFileState(id);
                    if (state?.Content is not null && state.Metadata.Publisher == publisher.ToString())
                    {
                        if (state.Metadata.IsHarvested)
                        {
                            return Results.BadRequest();
                        }

                        FileStorageResponse response = await client.GetFileStates(new FileStorageQuery
                        {
                            ParentFile = id,
                            OnlyTypes = new List<FileType> { FileType.DistributionRegistration },
                            MaxResults = 0
                        }).ConfigureAwait(false);
                        bool hasDistributions = response.TotalCount > 0;

                        DcatDataset? datasetRdf = DcatDataset.Parse(state.Content);
                        if (datasetRdf is not null)
                        {
                            ValidationResults validationResults = await dataset.Validate(publisherId, client, codelistProviderClient);
                            if (validationResults.IsValid)
                            {
                                Guid? parentDataset = null;
                                if (!string.IsNullOrEmpty(dataset.IsPartOf) && Guid.TryParse(dataset.IsPartOf, out Guid parentDatasetId))
                                {
                                    parentDataset = parentDatasetId;
                                }

                                datasetRdf.Modified = DateTimeOffset.UtcNow;
                                dataset.MapToRdf(publisher, datasetRdf);
                                FileMetadata metadata = datasetRdf.UpdateMetadata(hasDistributions || dataset.IsSerie, state.Metadata);
                                metadata = await datasetRdf.UpdateReferenceToParent(parentDataset, metadata, client);
                                await client.InsertFile(datasetRdf.ToString(), true, metadata).ConfigureAwait(false);
                                result.Id = metadata.Id.ToString();
                                result.Success = true;
                            }
                            else
                            {
                                result.Errors = validationResults;
                            }
                        }
                        else
                        {
                            return Results.Problem("Source rdf entity is not valid state");
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
            }
            else
            {
                result.Errors ??= new Dictionary<string, string>();
                result.Errors["generic"] = "Bad request";
            }
        }
        catch (Exception e)
        {
            telemetryClient?.TrackException(e);
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Generic error";
        }
        return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapDelete("/datasets", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            FileMetadata? metadata = await client.GetFileMetadata(key);
            if (metadata is not null && metadata.Type == FileType.DatasetRegistration && metadata.Publisher == publisher.ToString())
            {
                if (metadata.IsHarvested)
                {
                    return Results.BadRequest();
                }

                FileStorageResponse response = await client.GetFileStates(new FileStorageQuery
                {
                    ParentFile = key,
                    OnlyTypes = new List<FileType> { FileType.DatasetRegistration },
                    MaxResults = 0
                }).ConfigureAwait(false);

                if (response.TotalCount == 0)
                {
                    await client.DeleteFile(key).ConfigureAwait(false);
                    return Results.Ok();
                }
                else
                {
                    return Results.BadRequest("Dátovú sériu nie je možné zmazať, najskôr prosím zmažte všetky datasety z tejto série.");
                }
            }
        }

        return Results.Forbid();
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/distributions", [Authorize] async ([FromBody] DistributionInput? distribution, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (distribution is not null)
        {
            if (Guid.TryParse(distribution.DatasetId, out Guid datasetId))
            {
                FileState? datasetState = await client.GetFileState(datasetId).ConfigureAwait(false);
                if (datasetState?.Content is not null && datasetState.Metadata.Publisher == publisher.ToString())
                {
                    if (datasetState.Metadata.IsHarvested)
                    {
                        return Results.BadRequest();
                    }

                    DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);
                    if (dataset is not null)
                    {
                        FileMetadata? distributionFileMetadata = null;
                        if (distribution.FileId is not null)
                        {
                            if (Guid.TryParse(distribution.FileId, out Guid fileId))
                            {
                                distributionFileMetadata = await client.GetFileMetadata(fileId).ConfigureAwait(false);
                                if (distributionFileMetadata is null || distributionFileMetadata.Type != FileType.DistributionFile || distributionFileMetadata.Publisher != publisher.ToString())
                                {
                                    return Results.Forbid();
                                }
                            }
                            else
                            {
                                return Results.Forbid();
                            }
                        }

                        ValidationResults validationResults = await distribution.Validate(publisherId, client, codelistProviderClient);
                        if (validationResults.IsValid)
                        {
                            DcatDistribution distributionRdf = DcatDistribution.Create(datasetId);
                            distribution.MapToRdf(distributionRdf);
                            FileMetadata metadata = distributionRdf.UpdateMetadata(datasetState.Metadata);
                            await client.InsertFile(distributionRdf.ToString(), false, metadata).ConfigureAwait(false);
                            await client.UpdateDatasetMetadata(datasetId, true).ConfigureAwait(false);  
                            if (distributionFileMetadata is not null)
                            {
                                await client.UpdateMetadata(distributionFileMetadata with { ParentFile = metadata.Id }).ConfigureAwait(false);
                            }

                            result.Id = metadata.Id.ToString();
                            result.Success = true;
                        }
                        else
                        {
                            result.Errors = validationResults;
                        }
                    }
                    else
                    {
                        return Results.Problem("Source rdf entity is not valid state");
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
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPut("/distributions", [Authorize] async ([FromBody] DistributionInput distribution, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (distribution is not null)
        {
            if (Guid.TryParse(distribution.Id, out Guid id))
            {
                FileState? state = await client.GetFileState(id);
                if (state?.Content is not null && state.Metadata.Type == FileType.DistributionRegistration && state.Metadata.Publisher == publisher.ToString() && state.Metadata.ParentFile.HasValue)
                {
                    if (state.Metadata.IsHarvested)
                    {
                        return Results.BadRequest();
                    }

                    Guid datasetId = state.Metadata.ParentFile.Value;

                    DcatDistribution? distributionRdf = DcatDistribution.Parse(state.Content);
                    if (distributionRdf is not null)
                    {
                        FileState? datasetState = await client.GetFileState(datasetId).ConfigureAwait(false);
                        if (datasetState?.Content is not null && datasetState.Metadata.Publisher == publisher.ToString())
                        {
                            DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);
                            if (dataset is not null)
                            {
                                FileMetadata? distributionFileMetadata = null;
                                if (distribution.FileId is not null)
                                {
                                    if (Guid.TryParse(distribution.FileId, out Guid fileId))
                                    {
                                        distributionFileMetadata = await client.GetFileMetadata(fileId).ConfigureAwait(false);
                                        if (distributionFileMetadata is null || distributionFileMetadata.Type != FileType.DistributionFile || distributionFileMetadata.Publisher != publisher.ToString())
                                        {
                                            return Results.Forbid();
                                        }
                                    }
                                    else
                                    {
                                        return Results.Forbid();
                                    }
                                }

                                ValidationResults validationResults = await distribution.Validate(publisherId, client, codelistProviderClient);
                                if (validationResults.IsValid)
                                {
                                    distribution.MapToRdf(distributionRdf);
                                    FileMetadata metadata = distributionRdf.UpdateMetadata(datasetState.Metadata, state.Metadata);
                                    await client.InsertFile(distributionRdf.ToString(), true, metadata).ConfigureAwait(false);
                                    await client.UpdateDatasetMetadata(datasetId, true).ConfigureAwait(false);

                                    if (distributionFileMetadata is not null)
                                    {
                                        await client.UpdateMetadata(distributionFileMetadata with { ParentFile = metadata.Id }).ConfigureAwait(false);
                                    }

                                    result.Id = metadata.Id.ToString();
                                    result.Success = true;
                                }
                                else
                                {
                                    result.Errors = validationResults;
                                }
                            }
                            else
                            {
                                return Results.Problem("Source rdf entity is not valid state");
                            }
                        }
                        else
                        {
                            return Results.Forbid();
                        }
                    }
                    else
                    {
                        return Results.Problem("Source rdf entity is not valid state");
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
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPut("/distributions/licences", [Authorize] async ([FromBody] DistributionLicenceInput distribution, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (distribution is not null)
        {
            if (Guid.TryParse(distribution.Id, out Guid id))
            {
                FileState? state = await client.GetFileState(id);
                if (state?.Content is not null && state.Metadata.Type == FileType.DistributionRegistration && state.Metadata.Publisher == publisher.ToString() && state.Metadata.ParentFile.HasValue)
                {
                    if (state.Metadata.IsHarvested)
                    {
                        return Results.BadRequest();
                    }

                    Guid datasetId = state.Metadata.ParentFile.Value;

                    DcatDistribution? distributionRdf = DcatDistribution.Parse(state.Content);
                    if (distributionRdf is not null)
                    {
                        FileState? datasetState = await client.GetFileState(datasetId).ConfigureAwait(false);
                        if (datasetState?.Content is not null && datasetState.Metadata.Publisher == publisher.ToString())
                        {
                            DcatDataset? dataset = DcatDataset.Parse(datasetState.Content);
                            if (dataset is not null)
                            {
                                ValidationResults validationResults = await distribution.Validate(publisherId, client, codelistProviderClient);
                                if (validationResults.IsValid)
                                {
                                    FileMetadata? distributionFileMetadata = null;

                                    distribution.MapToRdf(distributionRdf);
                                    FileMetadata metadata = distributionRdf.UpdateMetadata(datasetState.Metadata, state.Metadata);
                                    await client.InsertFile(distributionRdf.ToString(), true, metadata).ConfigureAwait(false);
                                    await client.UpdateDatasetMetadata(datasetId, false).ConfigureAwait(false);

                                    if (distributionFileMetadata is not null)
                                    {
                                        await client.UpdateMetadata(distributionFileMetadata with { ParentFile = metadata.Id }).ConfigureAwait(false);
                                    }

                                    result.Id = metadata.Id.ToString();
                                    result.Success = true;
                                }
                                else
                                {
                                    result.Errors = validationResults;
                                }                                    
                            }
                            else
                            {
                                return Results.Problem("Source rdf entity is not valid state");
                            }
                        }
                        else
                        {
                            return Results.Forbid();
                        }
                    }
                    else
                    {
                        return Results.Problem("Source rdf entity is not valid state");
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
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapDelete("/distributions", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            FileMetadata? metadata = await client.GetFileMetadata(key);
            if (metadata is not null && metadata.Type == FileType.DistributionRegistration && metadata.Publisher == publisher.ToString() && metadata.ParentFile.HasValue)
            {
                if (metadata.IsHarvested)
                {
                    return Results.BadRequest();
                }

                Guid datasetId = metadata.ParentFile.Value;
                await client.DeleteFile(key).ConfigureAwait(false);
                await client.UpdateDatasetMetadata(datasetId, true).ConfigureAwait(false); 
            }

            return Results.Ok();
        }
        else
        {
            return Results.Forbid();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/local-catalogs", [Authorize] async ([FromBody] LocalCatalogInput? catalog, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (catalog is not null)
        {
            ValidationResults validationResults = await catalog.Validate(codelistProviderClient);
            if (validationResults.IsValid)
            {
                DcatCatalog catalogRdf = DcatCatalog.Create();
                catalog.MapToRdf(publisher, catalogRdf);
                FileMetadata metadata = catalogRdf.UpdateMetadata();
                await client.InsertFile(catalogRdf.ToString(), false, metadata).ConfigureAwait(false);
                result.Id = metadata.Id.ToString();
                result.Success = true;
            }
            else
            {
                result.Errors = validationResults;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPut("/local-catalogs", [Authorize] async ([FromBody] LocalCatalogInput? catalog, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (catalog is not null)
        {
            if (Guid.TryParse(catalog.Id, out Guid id))
            {
                FileState? state = await client.GetFileState(id);
                if (state?.Content is not null && state.Metadata.Publisher == publisher.ToString())
                {
                    DcatCatalog? catalogRdf = DcatCatalog.Parse(state.Content);
                    if (catalogRdf is not null)
                    {
                        ValidationResults validationResults = await catalog.Validate(codelistProviderClient);
                        if (validationResults.IsValid)
                        {
                            catalog.MapToRdf(publisher, catalogRdf);
                            FileMetadata metadata = catalogRdf.UpdateMetadata(state.Metadata);
                            await client.InsertFile(catalogRdf.ToString(), true, metadata).ConfigureAwait(false);
                            result.Id = metadata.Id.ToString();
                            result.Success = true;
                        }
                        else
                        {
                            result.Errors = validationResults;
                        }
                    }
                    else
                    {
                        return Results.Problem("Source rdf entity is not valid state");
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
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapDelete("/local-catalogs", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await client.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            await client.DeleteFile(key).ConfigureAwait(false);
            return Results.Ok();
        }
        else
        {
            return Results.Forbid();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/publishers", [Authorize] async ([FromBody] AdminPublisherInput? input, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (user.IsInRole("Superadmin"))
        {
            SaveResult result = new SaveResult();
            try
            {
                if (input is not null)
                {
                    ValidationResults validationResults = await input.Validate(codelistProviderClient);
                    if (validationResults.IsValid && input.Uri is not null)
                    {
                        FileState? existingPublisher = await client.GetPublisherFileState(input.Uri);
                        if (existingPublisher is null)
                        {
                            FoafAgent agent = FoafAgent.Create(new Uri(input.Uri));
                            input.MapToRdf(agent);
                            FileMetadata metadata = agent.UpdateMetadata();
                            metadata = metadata with { IsPublic = input.IsEnabled };
                            await client.InsertFile(agent.ToString(), false, metadata).ConfigureAwait(false);
                            result.Id = metadata.Id.ToString();
                            result.Success = true;
                        }
                        else
                        {
                            result.Errors ??= new Dictionary<string, string>();
                            result.Errors["generic"] = "Poskytovateľ dát už existuje";
                        }
                    }
                    else
                    {
                        result.Errors = validationResults;
                    }
                }
                else
                {
                    result.Errors ??= new Dictionary<string, string>();
                    result.Errors["generic"] = "Bad request";
                }
            }
            catch (Exception e)
            {
                telemetryClient?.TrackException(e);
                result.Errors ??= new Dictionary<string, string>();
                result.Errors["generic"] = "Generic error";
            }
            return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
        }
        else
        {
            return Results.Forbid();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPut("/publishers", [Authorize] async ([FromBody] AdminPublisherInput input, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (user.IsInRole("Superadmin"))
        {
            SaveResult result = new SaveResult();

            try
            {
                if (!string.IsNullOrEmpty(input?.Id) && Guid.TryParse(input.Id, out Guid id))
                {
                    FileState? state = await client.GetFileState(id).ConfigureAwait(false);
                    if (state is not null)
                    {
                        if (input is not null)
                        {
                            ValidationResults validationResults = await input.Validate(codelistProviderClient);
                            if (validationResults.IsValid && input.Uri is not null)
                            {
                                FileState? existingPublisher = await client.GetPublisherFileState(input.Uri);
                                if (existingPublisher is null || existingPublisher.Metadata.Id == state.Metadata.Id)
                                {
                                    FoafAgent agent = FoafAgent.Create(new Uri(input.Uri));
                                    input.MapToRdf(agent);
                                    FileMetadata metadata = agent.UpdateMetadata(state.Metadata);
                                    metadata = metadata with { IsPublic = input.IsEnabled };
                                    await client.InsertFile(agent.ToString(), true, metadata).ConfigureAwait(false);
                                    result.Id = metadata.Id.ToString();
                                    result.Success = true;
                                }
                                else
                                {
                                    result.Errors ??= new Dictionary<string, string>();
                                    result.Errors["generic"] = "Poskytovateľ dát už existuje";
                                }                                    
                            }
                            else
                            {
                                result.Errors = validationResults;
                            }
                        }
                        else
                        {
                            result.Errors ??= new Dictionary<string, string>();
                            result.Errors["generic"] = "Bad request";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                telemetryClient?.TrackException(e);
                result.Errors ??= new Dictionary<string, string>();
                result.Errors["generic"] = "Generic error";
            }
            return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
        }
        else
        {
            return Results.Forbid();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapDelete("publishers", [Authorize] async ([FromQuery] string? id, [FromServices] IDocumentStorageClient client, [FromServices] TelemetryClient? telemetryClient) =>
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
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("user-info", [Authorize] async ([FromServices] IDocumentStorageClient documentStorageClient, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        UserInfo userInfo = await client.GetUserInfo().ConfigureAwait(false);
        FoafAgent? agent = null;
        PublisherView? publisherView = null;
        bool publisherActive = false;
        if (userInfo.Publisher is not null)
        {
            FileState? state = await documentStorageClient.GetPublisherFileState(userInfo.Publisher).ConfigureAwait(false);
            if (state is not null)
            {
                publisherActive = state.Metadata.IsPublic;
                if (state.Content is not null)
                {
                    agent = FoafAgent.Parse(state.Content);
                    if (agent is not null)
                    {
                        publisherView = PublisherView.MapFromRdf(state.Metadata.Id, state.Metadata.IsPublic, 0, agent, null, "sk", true);
                    }
                }
            }
        }
        return Results.Ok(new WebApi.UserInfo
        {
            Id = userInfo.Id,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            Email = userInfo.Email,
            Role = userInfo.Role,
            Publisher = userInfo.Publisher,
            PublisherView = publisherView,
            PublisherEmail = agent?.EmailAddress,
            PublisherHomePage = agent?.HomePage?.ToString(),
            PublisherPhone = agent?.Phone,
            PublisherActive = publisherActive,
            PublisherLegalForm = agent?.LegalForm?.ToString()
        });
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("publishers/impersonate", [Authorize] async ([FromQuery] string? id, [FromServices] IIdentityAccessManagementClient client, [FromServices] IDocumentStorageClient documentStorageClient, ClaimsPrincipal user, HttpResponse response, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (user.IsInRole("Superadmin"))
        {
            if (!string.IsNullOrEmpty(id) && Guid.TryParse(id, out Guid fileId))
            {
                FileState? state = await documentStorageClient.GetFileState(fileId).ConfigureAwait(false);
                if (state?.Metadata.Publisher is not null)
                {
                    TokenResult token = await client.DelegatePublisher(state.Metadata.Publisher).ConfigureAwait(false);
                    string serializedToken = JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                    response.Cookies.Append("accessToken", serializedToken, new CookieOptions { HttpOnly = true });
                    return Results.Ok(token);
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
        }
        else
        {
            return Results.Forbid();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/codelists/search", [Authorize] async ([FromServices] ICodelistProviderClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        List<CodelistAdminView> items = new List<CodelistAdminView>();
        foreach (Codelist codelist in await client.GetCodelists().ConfigureAwait(false))
        {
            items.Add(new CodelistAdminView(codelist.Id, codelist.GetLabel("sk"), codelist.ItemsCount));
        }
        return Results.Ok(items);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/codelists", [Authorize] async ([FromServices] ICodelistProviderClient codelistProviderClient, IFormFile file, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    if (user.IsInRole("Superadmin"))
    {
        if (file is not null)
        {
            try
            {
                using Stream stream = file.OpenReadStream();
                await codelistProviderClient.UpdateCodelist(stream);
                return Results.Ok();
            }
            catch (HttpRequestException e)
            {
                telemetryClient?.TrackException(e);
                return e.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
                    System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
                    _ => Results.Problem()
                };
            }
            catch (Exception e)
            {
                telemetryClient?.TrackException(e);
                return Results.Problem();
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

app.MapPost("/users/search", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, UserInfoQuery query, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.GetUsers(query).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/users", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, NewUserInput input, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        input.FirstName = input.FirstName?.Trim() ?? string.Empty;
        input.LastName = input.LastName?.Trim() ?? string.Empty;
        input.Email = input.Email?.Trim();

        return Results.Ok(await client.CreateUser(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPut("/users", [Authorize] async ([FromServices] IIdentityAccessManagementClient client, EditUserInput input, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        input.FirstName = input.FirstName?.Trim() ?? string.Empty;
        input.LastName = input.LastName?.Trim() ?? string.Empty;
        input.Email = input.Email?.Trim();

        return Results.Ok(await client.UpdateUser(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapDelete("/users", [Authorize] async ([FromQuery] string? id, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    if (!string.IsNullOrEmpty(id))
    {
        try
        {
            await client.DeleteUser(id).ConfigureAwait(false);
            return Results.Ok();
        }
        catch (HttpRequestException e)
        {
            telemetryClient?.TrackException(e);
            return e.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
                System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
                _ => Results.Problem()
            };
        }
        catch (Exception e)
        {
            telemetryClient?.TrackException(e);
            return Results.Problem();
        }
    }
    return Results.NotFound();
});

app.MapPost("/registration", [Authorize] async ([FromServices] IDocumentStorageClient documentStorageClient, RegistrationInput? input, ClaimsPrincipal user, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    string? companyName = user?.Claims.FirstOrDefault(c => c.Type == "CompanyName")?.Value;
    if (string.IsNullOrEmpty(companyName))
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (input is not null)
        {
            ValidationResults validationResults = await input.Validate(codelistProviderClient);
            if (validationResults.IsValid)
            {
                FileState? state = await documentStorageClient.GetPublisherFileState(publisherId).ConfigureAwait(false);
                if (state is null)
                {
                    FoafAgent publisherRdf = FoafAgent.Create(publisher);
                    input.MapToRdf(publisherRdf, companyName);
                    FileMetadata metadata = publisherRdf.UpdateMetadata();
                    await documentStorageClient.InsertFile(publisherRdf.ToString(), false, metadata).ConfigureAwait(false);
                    result.Id = metadata.Id.ToString();
                    result.Success = true;
                }
                else
                {
                    result.Errors ??= new Dictionary<string, string>();
                    result.Errors["generic"] = "Zastupovaný poskytovateľ dát už má vytvorenú registráciu";
                }                
            }
            else
            {
                result.Errors = validationResults;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPut("/profile", [Authorize] async ([FromServices] IDocumentStorageClient documentStorageClient, RegistrationInput? input, ClaimsPrincipal user, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    string? publisherId = user?.Claims.FirstOrDefault(c => c.Type == "Publisher")?.Value;
    if (string.IsNullOrEmpty(publisherId) || !Uri.TryCreate(publisherId, UriKind.Absolute, out Uri? publisher) || publisher == null)
    {
        return Results.Forbid();
    }

    FileState? publisherState = await documentStorageClient.GetPublisherFileState(publisherId).ConfigureAwait(false);
    if (publisherState is null || !publisherState.Metadata.IsPublic)
    {
        return Results.Forbid();
    }

    SaveResult result = new SaveResult();
    try
    {
        if (input is not null)
        {
            ValidationResults validationResults = await input.Validate(codelistProviderClient);
            if (validationResults.IsValid)
            {
                FileState? state = await documentStorageClient.GetPublisherFileState(publisherId).ConfigureAwait(false);
                if (state?.Content is not null)
                {
                    FoafAgent? publisherRdf = FoafAgent.Parse(state.Content);
                    if (publisherRdf is not null)
                    {
                        input.MapToRdf(publisherRdf);
                        FileMetadata metadata = publisherRdf.UpdateMetadata(state.Metadata);
                        await documentStorageClient.InsertFile(publisherRdf.ToString(), true, metadata).ConfigureAwait(false);
                        result.Id = metadata.Id.ToString();
                        result.Success = true;
                    }
                    else
                    {
                        result.Errors ??= new Dictionary<string, string>();
                        result.Errors["generic"] = "Source rdf entity is not valid state";
                    }
                }
                else
                {
                    result.Errors ??= new Dictionary<string, string>();
                    result.Errors["generic"] = "Source rdf entity is not valid state";
                }
            }
            else
            {
                result.Errors = validationResults;
            }
        }
        else
        {
            result.Errors ??= new Dictionary<string, string>();
            result.Errors["generic"] = "Bad request";
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        result.Errors ??= new Dictionary<string, string>();
        result.Errors["generic"] = "Generic error";
    }
    return result.Errors is null ? Results.Ok(result) : Results.BadRequest(result);
});

app.MapPost("/upload", [Authorize] [RequestSizeLimit(314572800)] async ([FromServices] IDocumentStorageClient client, ClaimsPrincipal identity, HttpRequest request, IFormFile file, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (file is not null)
        {
            string? publisher = identity.FindFirstValue("Publisher");
            if (!string.IsNullOrEmpty(publisher))
            {
                if (file.Length > maxFileSize)
                {
                    return Results.BadRequest("File is too large");
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                FileMetadata metadata = new FileMetadata(Guid.NewGuid(), file.FileName, FileType.DistributionFile, null, publisher, true, file.FileName, now, now);
                using Stream stream = file.OpenReadStream();
                await client.UploadStream(stream, metadata, false).ConfigureAwait(false);
                return Results.Ok(new FileUploadResult
                {
                    Id = metadata.Id.ToString(),
                    Url = $"https://{request.Host}/download?id={HttpUtility.HtmlEncode(metadata.Id)}"
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
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<FileUploadResult>();

async Task<FileMetadata?> FindAndValidateDownload(IDocumentStorageClient client, Guid id)
{
    FileMetadata? metadata = await client.GetFileMetadata(id).ConfigureAwait(false);
    if (metadata is not null)
    {
        if (metadata.Type == FileType.DistributionFile && metadata.ParentFile.HasValue)
        {
            FileMetadata? distributionMetadata = await client.GetFileMetadata(metadata.ParentFile.Value).ConfigureAwait(false);
            if (distributionMetadata is not null && distributionMetadata.Type == FileType.DistributionRegistration && distributionMetadata.ParentFile.HasValue)
            {
                FileMetadata? datasetMetadata = await client.GetFileMetadata(distributionMetadata.ParentFile.Value).ConfigureAwait(false);
                if (datasetMetadata is not null && datasetMetadata.Type == FileType.DatasetRegistration)
                {
                    return metadata;
                }
            }
        }
    }
    return null;
}

app.MapMethods("/download", new[] { "HEAD" }, async ([FromServices] IDocumentStorageClient client, [FromQuery] string? id, HttpResponse response, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (Guid.TryParse(id, out Guid key))
        {
            FileMetadata? metadata = await FindAndValidateDownload(client, key).ConfigureAwait(false);
            if (metadata is not null)
            {
                ContentDisposition contentDisposition = new ContentDisposition
                {
                    DispositionType = "attachment",
                    FileName = metadata.OriginalFileName
                };
                response.Headers.ContentDisposition = contentDisposition.ToString();
                long? size = await client.GetSize(key).ConfigureAwait(false);
                if (size.HasValue)
                {
                    response.ContentLength = size;
                }
                return Results.Ok();
            }
        }
        return Results.NotFound();
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapGet("/download", async ([FromServices] IDocumentStorageClient client, [FromQuery] string? id, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        string language = "sk";
        if (Guid.TryParse(id, out Guid key))
        {
            FileMetadata? metadata = await FindAndValidateDownload(client, key).ConfigureAwait(false);
            if (metadata is not null)
            {
                Stream? stream = await client.DownloadStream(key).ConfigureAwait(false);
                if (stream is not null)
                {
                    FileStreamHttpResult r = TypedResults.File(stream, fileDownloadName: metadata.OriginalFileName ?? metadata.Name.GetText(language) ?? metadata.Id.ToString());
                    return r;
                }
            }
        }
        return Results.NotFound();
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/refresh", async ([FromServices] IIdentityAccessManagementClient client, RefreshTokenRequest request, HttpResponse response, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (!string.IsNullOrEmpty(request?.AccessToken) && !string.IsNullOrEmpty(request?.RefreshToken))
        {
            TokenResult token = await client.RefreshToken(request.AccessToken, request.RefreshToken);
            if (!string.IsNullOrEmpty(token.Token))
            {
                string serializedToken = JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                response.Cookies.Append("accessToken", serializedToken, new CookieOptions { HttpOnly = true });
            }

            return Results.Ok(token);
        }
        else
        {
            return Results.BadRequest();
        }
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapGet("/saml/login", async ([FromQuery] string? method, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.GetLogin(method));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<DelegationAuthorizationResult>();

app.MapGet("/saml/logout", async ([FromServices] IIdentityAccessManagementClient client, HttpRequest request, HttpResponse response, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        DelegationAuthorizationResult? result = await client.Logout(request.QueryString.ToString());
        if (result is null || result.DoLogout)
        {
            response.Cookies.Delete("accessToken");
        }
        
        if (request.Headers.Accept.Any(s => s?.Contains("application/json") ?? false))
        {
            return Results.Ok(result);
        }
        else
        {
            return Results.Redirect(result?.RedirectUrl ?? "/");
        }
    }
    catch (HttpRequestException e)
    {
        response.Cookies.Delete("accessToken");
        if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized || e.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return Results.Redirect("/");
        }

        telemetryClient?.TrackException(e);
        return Results.Redirect("/");
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Redirect("/");
    }
});

app.MapPost("/saml/consume", async ([FromServices] IIdentityAccessManagementClient client, HttpRequest request, HttpResponse response, IWebHostEnvironment environment, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        using StreamReader reader = new StreamReader(request.Body);
        TokenResult token = await client.Consume(await reader.ReadToEndAsync()).ConfigureAwait(false);
        string serializedToken = JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });

        response.Cookies.Append("accessToken", serializedToken, new CookieOptions { HttpOnly = true });

        string mainPage = Path.Combine(environment.WebRootPath, "index.html");
        string content = string.Empty;
        if (File.Exists(mainPage))
        {
            content = File.ReadAllText(mainPage);
            content = content.Replace("<script>var externalToken=null</script>", $"<script>var externalToken = {serializedToken};</script>");
        }
        return Results.Content(content, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/html"));
    }
    catch (HttpRequestException e)
    {
        if (e.StatusCode == System.Net.HttpStatusCode.Unauthorized || e.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return Results.Redirect("/");
        }

        telemetryClient?.TrackException(e);
        return Results.Redirect("/");
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Redirect("/");
    }
});

app.MapGet("/sparql-endpoint-url", (IConfiguration configuration) =>
{
    return Results.Ok(configuration["PublicSparqlEndpoint"]);
});

app.MapGet("/validate-inviation", async ([FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.CheckInvitation().ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
});

app.MapPost("/users/login", async ([FromBody] LoginInput? input, HttpResponse response, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        TokenResult token = await client.Login(input).ConfigureAwait(false);
        string serializedToken = JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        response.Cookies.Append("accessToken", serializedToken, new CookieOptions { HttpOnly = true });

        return Results.Ok(token);
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<TokenResult>().Produces<EmptyResult>(403);

app.MapPost("/users/register", async ([FromBody] UserRegistrationInput? input, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.Register(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<SaveResult>();

app.MapPost("/users/activation", async ([FromBody] ActivationInput? input, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.ActivateAccount(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<SaveResult>();

app.MapPost("/users/recovery", async ([FromBody] PasswordRecoveryInput? input, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.RequestPasswordRecovery(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<SaveResult>();

app.MapPost("/users/recovery-activation", async ([FromBody] PasswordRecoveryConfirmationInput? input, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.ConfirmPasswordRecovery(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<SaveResult>();

app.MapPost("/users/change-password", async ([FromBody] PasswordChangeInput? input, [FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.ChangePassword(input).ConfigureAwait(false));
    }
    catch (HttpRequestException e)
    {
        telemetryClient?.TrackException(e);
        return e.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => Results.StatusCode((int)e.StatusCode),
            System.Net.HttpStatusCode.Forbidden => Results.StatusCode((int)e.StatusCode),
            _ => Results.Problem()
        };
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
        return Results.Problem();
    }
}).Produces<SaveResult>();

app.MapGet("/signin-google", async ([FromQuery] string? code, string? state, [FromServices] IIdentityAccessManagementClient client, HttpResponse response, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        TokenResult? token = await client.SignGoogle(code, state).ConfigureAwait(false);
        if (token is not null)
        {
            string serializedToken = JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            response.Cookies.Append("accessToken", serializedToken, new CookieOptions { HttpOnly = true });
        }
    }
    catch (Exception e)
    {
        telemetryClient?.TrackException(e);
    }
    return Results.Redirect("/");
}).Produces<SaveResult>();

app.Use(async (context, next) =>
{
    if (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put)
    {
        IConfiguration configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        string? logPath = configuration["LogPath"];
        if (!string.IsNullOrEmpty(logPath) && Directory.Exists(logPath))
        {
            context.Request.EnableBuffering();
            string logName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss.fffff}_{Guid.NewGuid():N}";
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

app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is not null || HttpMethods.IsGet(context.Request?.Method ?? string.Empty))
    {
        await next(context);
        return;
    }
    context.Response.Redirect(context.Request.GetUri().ToString());
    return;
});

app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is null)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        string[] prefixes = new[] { "/set/", "/dataset/", "/datasety/" };

        string prefix = prefixes.FirstOrDefault(path.StartsWith) ?? string.Empty;
        if (!string.IsNullOrEmpty(prefix))
        {
            path = path.Substring(prefix.Length);

            IDocumentStorageClient client = context.RequestServices.GetRequiredService<IDocumentStorageClient>();

            Uri BuildUri(string prefix)
            {
                UriBuilder uriBuilder = new UriBuilder();
                uriBuilder.Scheme = "https";
                uriBuilder.Host = "data.gov.sk";
                uriBuilder.Path = prefix + path;
                return uriBuilder.Uri;
            }

            async Task<bool> TryFindByQuery(Action<FileStorageQuery> queryDecorator)
            {
                FileStorageQuery query = new FileStorageQuery
                {
                    MaxResults = 1,
                    OnlyTypes = new List<FileType> { FileType.DatasetRegistration, FileType.DistributionRegistration },
                    AdditionalFilters = new Dictionary<string, string[]>(),
                };
                queryDecorator(query);
                FileStorageResponse response = await client.GetFileStates(query);
                if (response.Files.Count >= 1)
                {
                    FileState state = response.Files[0];
                    if (state.Metadata.Type == FileType.DatasetRegistration)
                    {
                        context.Response.Redirect($"/datasety/{state.Metadata.Id}");
                        return true;
                    }
                    else if (state.Metadata.Type == FileType.DistributionRegistration && state.Metadata.ParentFile.HasValue)
                    {
                        context.Response.Redirect($"/datasety/{state.Metadata.ParentFile.Value}");
                        return true;
                    }
                }
                return false;
            }

            Task<bool> TryFindByKey(Uri uri)
            {
                return TryFindByQuery(q =>
                {
                    q.AdditionalFilters ??= new Dictionary<string, string[]>();
                    q.AdditionalFilters["key"] = new[] { uri.ToString() };
                });
            }

            Task<bool> TryFindByLandingPage(Uri uri)
            {
                return TryFindByQuery(q =>
                {
                    q.AdditionalFilters ??= new Dictionary<string, string[]>();
                    q.AdditionalFilters["landingPage"] = new[] { uri.ToString() };
                });
            }

            bool result;

            if (prefix == "/set/")
            {
                result = await TryFindByKey(BuildUri("/set/"));
                if (result)
                {
                    return;
                }
            }

            result = await TryFindByLandingPage(BuildUri("/dataset/"));
            if (result)
            {
                return;
            }

            result = await TryFindByLandingPage(BuildUri("/datasety/"));
            if (result)
            {
                return;
            }

            result = await TryFindByLandingPage(BuildUri("/set/"));
            if (result)
            {
                return;
            }
        }
    }

    await next(context);
    return;
});

app.Use(async (context, next) =>
{
    if (context.GetEndpoint() is not null)
    {
        await next(context);
        return;
    }

    IWebHostEnvironment environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    string mainPage = Path.Combine(environment.WebRootPath, "index.html");
    string content = string.Empty;
    if (File.Exists(mainPage))
    {
        content = File.ReadAllText(mainPage);

        string? serializedToken = context.Request.Cookies["accessToken"];
        if (!string.IsNullOrWhiteSpace(serializedToken))
        {
            try
            {
                TokenResult? token = JsonConvert.DeserializeObject<TokenResult>(serializedToken);
                if (token is not null)
                {
                    content = content.Replace("<script>var externalToken=null</script>", $"<script>var externalToken = {JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })};</script>");
                }
            }
            catch
            {
                //ignore
            }
        }
        context.Response.Headers.ContentType = "text/html";
        await context.Response.WriteAsync(content);
    }
    else
    {
        await next(context);
    }
});

app.Run();

public partial class Program { }
