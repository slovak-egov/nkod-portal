
using NkodSk.Abstractions;

namespace WebApi
{
    public class ImportHarvestedHostedService : IHostedService
    {
        private readonly string documentStorageUrl;

        private readonly string iamUrl;

        private readonly string authToken;

        private readonly string sparqlEndpointUrl;

        private Task lastWorkTask = Task.CompletedTask;

        private Timer? timer;

        public ImportHarvestedHostedService(string documentStorageUrl, string iamUrl, string authToken, string sparqlEndpointUrl)
        {
            this.documentStorageUrl = documentStorageUrl;
            this.iamUrl = iamUrl;
            this.authToken = authToken;
            this.sparqlEndpointUrl = sparqlEndpointUrl;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await lastWorkTask;
            timer?.Dispose();
            timer = null;
        }

        private void OnTimerTick(object? state)
        {
            lastWorkTask = Task.Run(() => ExecuteAsync(CancellationToken.None));
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
                    httpClient.BaseAddress = new Uri(iamUrl);
                    return httpClient;
                }), httpContextValueAccessor);

            HttpClientHandler httpClientHandler = new HttpClientHandler
            {
                SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12
            };
            HttpClient httpClient = new HttpClient(httpClientHandler);
            httpClient.BaseAddress = new Uri(sparqlEndpointUrl);

            SparqlClient sparqlClient = new SparqlClient(httpClient);

            HarvestedDataImport dataImport = new HarvestedDataImport(sparqlClient, documentStorageClient, async p =>
            {
                string token = await iamClient.LoginHarvester(authToken, p);
                Console.WriteLine($"Token: {token}");
                httpContextValueAccessor.Token = token;
                httpContextValueAccessor.Publisher = p;
            }, Console.WriteLine);
            await dataImport.Import();
        }

        private class HttpContextValueAccessor : IHttpContextValueAccessor
        {
            public string? Publisher { get; set; }

            public string? Token { get; set; }

            public string? UserId => null;

            public bool HasRole(string role)
            {
                return string.Equals(role, "Harvester", StringComparison.OrdinalIgnoreCase);
            }
        }

        private class DefaultHttpClientFactory : IHttpClientFactory
        {
            private readonly Func<HttpClient> factory;

            public DefaultHttpClientFactory(Func<HttpClient> factory)
            {
                this.factory = factory;
            }

            public HttpClient CreateClient(string name)
            {
                return factory();
            }
        }
    }
}
