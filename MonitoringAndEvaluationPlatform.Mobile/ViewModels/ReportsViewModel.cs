using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public ReportsViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        [ObservableProperty]
        private ReportsSummary? summary;

        [ObservableProperty]
        private TopBottomPerformers? topPerformers;

        [ObservableProperty]
        private TopBottomPerformers? bottomPerformers;

        [ObservableProperty]
        private BudgetOverview? budgetOverview;

        [ObservableProperty]
        private PerformanceDistribution? distribution;

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Summary = await _apiService.GetReportsSummaryAsync();
                TopPerformers = await _apiService.GetTopPerformersAsync();
                BottomPerformers = await _apiService.GetBottomPerformersAsync();
                BudgetOverview = await _apiService.GetBudgetOverviewAsync();
                Distribution = await _apiService.GetPerformanceDistributionAsync();
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
