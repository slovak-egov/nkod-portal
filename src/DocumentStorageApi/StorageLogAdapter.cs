using Microsoft.ApplicationInsights;
using NkodSk.Abstractions;

namespace DocumentStorageApi
{
    public class StorageLogAdapter : IStorageLogAdapter
    {
        private readonly TelemetryClient telemetryClient;

        public StorageLogAdapter(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        public void LogFileCreated(string path)
        {
            telemetryClient.TrackEvent("Storage log file created", new Dictionary<string, string>
            {
                { "path", path }
            });
        }
    }
}
