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
});
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
        FileState fileState = response.Files[0];
        if (fileState.Content is not null)
        {
            FoafAgent? agent = FoafAgent.Parse(fileState.Content);
            if (agent is not null)
            {
                publishers[agent.Uri.ToString()] = PublisherView.MapFromRdf(fileState.Metadata.Id, fileState.Metadata.IsPublic, 0, agent, null, language);
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
                    response.Items.Add(PublisherView.MapFromRdf(group.PublisherFileState.Metadata.Id, group.PublisherFileState.Metadata.IsPublic, group.Count, agent, group.Themes, language));
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
});

app.MapPost("/datasets/search", async ([FromBody] DatasetQuery query, [FromServices] IDocumentStorageClient client, [FromServices] ICodelistProviderClient codelistProviderClient, ClaimsPrincipal? user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        bool isAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        string language = query.Language ?? "sk";
        FileStorageResponse storageResponse = await GetStorageResponse(query, language, q =>
        {
            q.OnlyTypes = new List<FileType> { FileType.DatasetRegistration };
            q.IncludeDependentFiles = true;
            return q;
        }, client, isAuthenticated).ConfigureAwait(false);
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
                    foreach (FileState dependedState in fileState.DependentFiles)
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
});

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
});

app.MapPost("/local-catalogs/search", async ([FromBody] LocalCatalogsQuery query, [FromServices] IDocumentStorageClient client, ClaimsPrincipal? user, [FromServices] TelemetryClient? telemetryClient) =>
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
                    LocalCatalogView view = LocalCatalogView.MapFromRdf(fileState.Metadata, catalogRdf, language, isAuthenticated);

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
});

app.MapGet("/codelists", async ([FromQuery(Name = "keys[]")] string[] keys, [FromServices] ICodelistProviderClient codelistProviderClient, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
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
                values.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label));
                codelists.Add(new CodelistView(codelist.Id, codelist.GetLabel(language), values));
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
});

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
});

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
                DcatDataset datasetRdf = DcatDataset.Create(new Uri($"http://data.gov.sk/dataset/{Guid.NewGuid()}"));
                dataset.MapToRdf(publisher, datasetRdf);
                FileMetadata metadata = datasetRdf.UpdateMetadata(false);
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
                                dataset.MapToRdf(publisher, datasetRdf);
                                FileMetadata metadata = datasetRdf.UpdateMetadata(hasDistributions, state.Metadata);
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
                            DcatDistribution distributionRdf = DcatDistribution.Create(new Uri($"http://data.gov.sk/distribution/{Guid.NewGuid()}"));
                            distribution.MapToRdf(distributionRdf);
                            FileMetadata metadata = distributionRdf.UpdateMetadata(datasetState.Metadata);
                            await client.InsertFile(distributionRdf.ToString(), false, metadata).ConfigureAwait(false);
                            await client.UpdateMetadata(dataset.UpdateMetadata(true, datasetState.Metadata)).ConfigureAwait(false);
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
            ValidationResults validationResults = catalog.Validate();
            if (validationResults.IsValid)
            {
                DcatCatalog catalogRdf = DcatCatalog.Create(new Uri($"http://data.gov.sk/catalog/{Guid.NewGuid()}"));
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
                        ValidationResults validationResults = catalog.Validate();
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

app.MapPut("/publishers", [Authorize] async ([FromBody] PublisherInput input, [FromServices] IDocumentStorageClient client, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (user.IsInRole("Superadmin"))
        {
            if (!string.IsNullOrEmpty(input?.PublisherId) && Guid.TryParse(input.PublisherId, out Guid id))
            {
                FileState? state = await client.GetFileState(id).ConfigureAwait(false);
                if (state is not null)
                {
                    FileMetadata metadata = state.Metadata with { IsPublic = input.IsEnabled };
                    await client.UpdateMetadata(metadata).ConfigureAwait(false);

                    if (!input.IsEnabled && state.Metadata.Publisher is not null)
                    {
                        FileStorageQuery query = new FileStorageQuery
                        {
                            OnlyPublished = true,
                            OnlyPublishers = new List<string> { state.Metadata.Publisher },
                            OnlyTypes = new List<FileType> { FileType.DatasetRegistration, FileType.LocalCatalogRegistration }
                        };
                        FileStorageResponse response = await client.GetFileStates(query).ConfigureAwait(false);
                        foreach (FileState fileState in response.Files)
                        {
                            await client.UpdateMetadata(fileState.Metadata with { IsPublic = false }).ConfigureAwait(false);
                        }
                    }

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
                        publisherView = PublisherView.MapFromRdf(state.Metadata.Id, state.Metadata.IsPublic, 0, agent, null, "sk");
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
            PublisherActive = publisherActive
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

app.MapPost("publishers/impersonate", [Authorize] async ([FromQuery] string? id, [FromServices] IIdentityAccessManagementClient client, [FromServices] IDocumentStorageClient documentStorageClient, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
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
                    return Results.Ok(await client.DelegatePublisher(state.Metadata.Publisher).ConfigureAwait(false));
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

app.MapPut("/codelists", [Authorize] async ([FromServices] ICodelistProviderClient codelistProviderClient, IFormFile file, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
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

app.MapPost("/registration", [Authorize] async ([FromServices] IDocumentStorageClient documentStorageClient, RegistrationInput? input, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
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
            ValidationResults validationResults = input.Validate();
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

app.MapPut("/profile", [Authorize] async ([FromServices] IDocumentStorageClient documentStorageClient, RegistrationInput? input, ClaimsPrincipal user, [FromServices] TelemetryClient? telemetryClient) =>
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
            ValidationResults validationResults = input.Validate();
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

app.MapPost("/upload", [Authorize] async ([FromServices] IDocumentStorageClient client, ClaimsPrincipal identity, HttpRequest request, IFormFile file, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
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

app.MapGet("/download", async ([FromServices] IDocumentStorageClient client, [FromQuery] string? id, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
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

app.MapPost("/refresh", async ([FromServices] IIdentityAccessManagementClient client, RefreshTokenRequest request, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        if (!string.IsNullOrEmpty(request?.AccessToken) && !string.IsNullOrEmpty(request?.RefreshToken))
        {
            return Results.Ok(await client.RefreshToken(request.AccessToken, request.RefreshToken));
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

app.MapGet("/saml/login", async ([FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        return Results.Ok(await client.GetLogin());
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

app.MapGet("/saml/logout", async ([FromServices] IIdentityAccessManagementClient client, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        await client.Logout();
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
});

app.MapPost("/saml/consume", async ([FromServices] IIdentityAccessManagementClient client, HttpRequest request, [FromServices] TelemetryClient? telemetryClient) =>
{
    try
    {
        using StreamReader reader = new StreamReader(request.Body);
        TokenResult token = await client.Consume(await reader.ReadToEndAsync()).ConfigureAwait(false);
        const string mainPage = "wwwroot/index.html";
        string content = string.Empty;
        if (File.Exists(mainPage))
        {
            content = File.ReadAllText(mainPage);
            content = content.Replace("<script>var externalToken=null</script>", $"<script>var externalToken = {JsonConvert.SerializeObject(token, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })};</script>");
        }
        return Results.Content(content, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/html"));
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

app.UseSpa(config =>
{
    
});

app.Run();

public partial class Program { }