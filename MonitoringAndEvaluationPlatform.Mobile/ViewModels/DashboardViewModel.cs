using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public DashboardViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private DashboardSummary? summary;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        public ObservableCollection<FrameworkPerformance> Frameworks { get; } = new();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Summary = await _apiService.GetDashboardSummaryAsync();

                var frameworks = await _apiService.GetFrameworksPerformanceAsync();
                Frameworks.Clear();
                if (frameworks != null)
                    foreach (var f in frameworks)
                        Frameworks.Add(f);
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
    }
}
