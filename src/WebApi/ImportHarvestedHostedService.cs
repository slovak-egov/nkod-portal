
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

        private Timer? onceTimer;

        private Timer? periodicTimer;

        public ImportHarvestedHostedService(string documentStorageUrl, string iamUrl, string authToken, string sparqlEndpointUrl)
        {
            this.documentStorageUrl = documentStorageUrl;
            this.iamUrl = iamUrl;
            this.authToken = authToken;
            this.sparqlEndpointUrl = sparqlEndpointUrl;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            onceTimer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
            
            DateTime now = DateTime.Now;
            DateTime firstRun = now.Date.AddDays(now.Hour >= 6 ? 1 : 0).AddHours(6);
            TimeSpan delay = firstRun - now;
            if (delay.TotalMinutes < 10)
            {
                delay = TimeSpan.FromMinutes(10);
            }
            
            periodicTimer = new Timer(OnTimerTick, null, delay, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await lastWorkTask;
            onceTimer?.Dispose();
            periodicTimer?.Dispose();
        }

        private async void OnTimerTick(object? state)
        {
            await lastWorkTask;
            lastWorkTask = Task.Run(() => ExecuteAsync(CancellationToken.None, Console.WriteLine));
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken, Action<string> logger)
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
            httpClient.Timeout = TimeSpan.FromSeconds(300);

            SparqlClient sparqlClient = new SparqlClient(httpClient);

            HarvestedDataImport dataImport = new HarvestedDataImport(sparqlClient, documentStorageClient, async p =>
            {
                string token = await iamClient.LoginHarvester(authToken, p);
                logger($"Token: {token}");
                httpContextValueAccessor.Token = token;
                httpContextValueAccessor.Publisher = p;
            }, logger);
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
