namespace MonitoringAndEvaluationPlatform.Mobile.Services
{
    public class SettingsService
    {
        private const string ServerUrlKey = "server_url";
        private const string LanguageKey = "language";
        private const string DefaultServerUrl = "https://localhost:7174";

        public string ServerUrl
        {
            get => Preferences.Get(ServerUrlKey, DefaultServerUrl);
            set => Preferences.Set(ServerUrlKey, value);
        }

        public string Language
        {
            get => Preferences.Get(LanguageKey, "ar");
            set => Preferences.Set(LanguageKey, value);
        }
    }
}
