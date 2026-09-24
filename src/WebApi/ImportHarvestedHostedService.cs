
using Microsoft.Extensions.ObjectPool;
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

        private DateOnly lastStarted;

        private readonly TimeSpan scheduledTime = new TimeSpan(6, 0, 0);

        private Timer? timer;

        public ImportHarvestedHostedService(string documentStorageUrl, string iamUrl, string authToken, string sparqlEndpointUrl)
        {
            this.documentStorageUrl = documentStorageUrl;
            this.iamUrl = iamUrl;
            this.authToken = authToken;
            this.sparqlEndpointUrl = sparqlEndpointUrl;

            DateTime now = DateTime.Now;
            lastStarted = DateOnly.FromDateTime(now);
            if (now.TimeOfDay < scheduledTime)
            {
                lastStarted = lastStarted.AddDays(-1);
            }
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            timer?.Change(Timeout.Infinite, Timeout.Infinite);
            await lastWorkTask;
        }

        private void OnTimerTick(object? state)
        {
            DateTime now = DateTime.Now;
            DateOnly today = DateOnly.FromDateTime(now);
            if (today > lastStarted && now.TimeOfDay >= scheduledTime)
            {
                lastStarted = today;
                lastWorkTask = ExecuteAsync(CancellationToken.None, Console.WriteLine);
            }
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken, Action<string> logger)
        {
            HttpContextValueAccessor httpContextValueAccessor = new HttpContextValueAccessor();

            HttpClient storageHttpClient = new HttpClient
            {
                BaseAddress = new Uri(documentStorageUrl)
            };

            DocumentStorageClient.DocumentStorageClient documentStorageClient = new DocumentStorageClient.DocumentStorageClient(
                new DefaultHttpClientFactory(() =>
                {
                    storageHttpClient.DefaultRequestHeaders.Authorization = null;
                    return storageHttpClient;
                }),
                httpContextValueAccessor);

            IAMClient.IdentityAccessManagementClient iamClient = new IAMClient.IdentityAccessManagementClient(
                new DefaultHttpClientFactory(() =>
                {
                    HttpClient httpClient = new HttpClient();
                    httpClient.BaseAddress = new Uri(iamUrl);
                    return httpClient;
                }), httpContextValueAccessor);

            SocketsHttpHandler httpHandler = new SocketsHttpHandler
            {
                PooledConnectionIdleTimeout = TimeSpan.FromHours(1),
                PooledConnectionLifetime = TimeSpan.FromHours(1),
                MaxConnectionsPerServer = 10
            };
            httpHandler.SslOptions.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;
            HttpClient httpClient = new HttpClient(httpHandler);
            httpClient.BaseAddress = new Uri(sparqlEndpointUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(300);

            SparqlClient sparqlClient = new SparqlClient(httpClient);

            HarvestedDataImport dataImport = new HarvestedDataImport(sparqlClient, documentStorageClient, async p =>
            {
                string token = await iamClient.LoginHarvester(authToken, p);
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
