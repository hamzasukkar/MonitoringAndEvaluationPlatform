using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class FrameworkPerformanceViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public FrameworkPerformanceViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private LookupItemInt? selectedMinistry;

        public ObservableCollection<FrameworkPerformance> Frameworks { get; } = new();
        public ObservableCollection<LookupItemInt> Ministries { get; } = new();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var ministries = await _apiService.GetMinistriesAsync();
                Ministries.Clear();
                if (ministries != null)
                    foreach (var m in ministries) Ministries.Add(m);

                await RefreshFrameworksAsync();
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        [RelayCommand]
        private async Task RefreshFrameworksAsync()
        {
            var frameworks = await _apiService.GetFrameworksPerformanceAsync(
                ministryCode: SelectedMinistry?.Code);

            Frameworks.Clear();
            if (frameworks != null)
                foreach (var f in frameworks) Frameworks.Add(f);
        }
    }
}
