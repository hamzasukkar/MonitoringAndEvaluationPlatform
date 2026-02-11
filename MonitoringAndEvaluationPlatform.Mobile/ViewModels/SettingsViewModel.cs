using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Services;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly AuthService _authService;

        public SettingsViewModel(SettingsService settingsService, AuthService authService)
        {
            _settingsService = settingsService;
            _authService = authService;
            serverUrl = _settingsService.ServerUrl;
            selectedLanguage = _settingsService.Language;
        }

        [ObservableProperty]
        private string serverUrl;

        [ObservableProperty]
        private string selectedLanguage;

        public List<string> Languages { get; } = new() { "ar", "en", "fr" };

        [RelayCommand]
        private void SaveSettings()
        {
            _settingsService.ServerUrl = ServerUrl;
            _settingsService.Language = SelectedLanguage;
            Shell.Current.DisplayAlert("Settings", "Settings saved successfully.", "OK");
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            _authService.Logout();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
