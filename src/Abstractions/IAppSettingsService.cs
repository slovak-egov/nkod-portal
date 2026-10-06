namespace NkodSk.Abstractions
{
    public interface IAppSettingsService
    {
        bool IsIframeHostAllowed(string host);

        AppSettings GetSettings();

        void SaveSettings(AppSettings settings);
    }
}
