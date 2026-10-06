using Microsoft.ApplicationInsights;
using NkodSk.Abstractions;
using System.Text.Json;

namespace WebApi
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly string filePath;
        private readonly TelemetryClient? telemetryClient;
        private AppSettings? cachedSettings;
        private readonly object syncLock = new object();

        public AppSettingsService(string filePath, TelemetryClient? telemetryClient = null)
        {
            this.filePath = filePath;
            this.telemetryClient = telemetryClient;
        }

        public bool IsIframeHostAllowed(string host) => GetSettings().IsHostAllowed(host);

        public AppSettings GetSettings()
        {
            lock (syncLock)
            {
                if (cachedSettings is null)
                {
                    cachedSettings = LoadFromFile();
                }
                return new AppSettings { AllowedIframeHosts = cachedSettings.AllowedIframeHosts };
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            lock (syncLock)
            {
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
                cachedSettings = settings;
            }
        }

        private AppSettings LoadFromFile()
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception e)
            {
                telemetryClient?.TrackException(e);
            }
            return new AppSettings();
        }
    }
}
