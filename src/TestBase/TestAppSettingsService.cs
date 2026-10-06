using NkodSk.Abstractions;

namespace TestBase
{
    public class TestAppSettingsService : IAppSettingsService
    {
        private AppSettings settings = new AppSettings();

        public AppSettings GetSettings() => settings;

        public bool IsIframeHostAllowed(string host) => settings.IsHostAllowed(host);

        public void SaveSettings(AppSettings settings)
        {
            this.settings = settings;
        }
    }
}
