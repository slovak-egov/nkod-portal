using ImportRegistrations;
using Lucene.Net.Util.Fst;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using NkodSk.Abstractions;

IConfiguration configuration = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
  .AddUserSecrets<Program>()
  .AddEnvironmentVariables()
  .AddCommandLine(args)
  .Build();


string? documentStorageUrl = configuration["DocumentStorageUrl"];
if (!Uri.IsWellFormedUriString(documentStorageUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get DocumentStorageUrl");
}

string? iamClientUrl = configuration["IAMUrl"];
if (!Uri.IsWellFormedUriString(iamClientUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get IAMUrl");
}

string? authToken = configuration["AuthToken"];
if (string.IsNullOrEmpty(authToken))
{
    throw new Exception("Unable to get AuthToken");
}

string? sparqlEndpointUrl = configuration["SparqlEndpointUrl"];
if (!Uri.IsWellFormedUriString(sparqlEndpointUrl, UriKind.Absolute))
{
    throw new Exception("Unable to get SparqlEndpointUrl");
}

HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

DocumentStorageClient.DocumentStorageClient documentStorageClient = new DocumentStorageClient.DocumentStorageClient(
    new DefaultHttpClientFactory(() =>
    {
        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(documentStorageUrl);
        return httpClient;
    }),
    httpContextValueAccessor);

IAMClient.IdentityAccessManagementClient iamClient = new IAMClient.IdentityAccessManagementClient(
    new DefaultHttpClientFactory(() =>
    {
    HttpClient httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(iamClientUrl);
    return httpClient;
}), httpContextValueAccessor);

HttpClientHandler httpClientHandler = new HttpClientHandler
{
    SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
};
HttpClient httpClient = new HttpClient(httpClientHandler);
httpClient.BaseAddress = new Uri(sparqlEndpointUrl);

SparqlClient sparqlClient = new SparqlClient(httpClient);

HarvestedDataImport dataImport = new HarvestedDataImport(sparqlClient, documentStorageClient, p => iamClient.LoginHarvester(authToken, p), Console.WriteLine);
await dataImport.Import();