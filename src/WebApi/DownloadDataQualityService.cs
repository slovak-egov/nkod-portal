using Microsoft.ApplicationInsights;
using NkodSk.Abstractions;

namespace WebApi
{
    public class DownloadDataQualityService
    {
        private Dictionary<string, bool>? status;

        private readonly ISparqlClient client;

        private TelemetryClient telemetryClient;

        private Task lastWorkTask = Task.CompletedTask;

        public DownloadDataQualityService(ISparqlClient client, TelemetryClient telemetryClient)
        {
            this.client = client;
            this.telemetryClient = telemetryClient;
            Timer? onceTimer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(30));
        }

        private async void OnTimerTick(object? state)
        {
            await lastWorkTask;
            lastWorkTask = Task.Run(Load);
        }

        public async Task Load()
        {            
            try
            {
                telemetryClient.TrackTrace("DownloadDataQualityService.Load");
                status = await this.client.GetDownloadQuality();
                telemetryClient.TrackTrace("DownloadDataQualityService.Loaded");
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
            }
        }

        public bool? IsDownloadQualityGood(string downloadUrl)
        {
            if (status == null)
            {
                return null;
            }
            if (status.TryGetValue(downloadUrl, out bool isGood))
            {
                return isGood;
            }
            return null;
        }
    }
}
