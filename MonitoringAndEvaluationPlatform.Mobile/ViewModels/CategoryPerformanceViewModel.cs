using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class CategoryPerformanceViewModel : ObservableObject
    {
        private readonly ApiService _apiService;
        private readonly string _category;

        public CategoryPerformanceViewModel(ApiService apiService, string category)
        {
            _apiService = apiService;
            _category = category;
            Title = category + " Performance";
        }

        [ObservableProperty]
        private string title;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        public ObservableCollection<CategoryReport> Items { get; } = new();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var data = _category switch
                {
                    "Ministry" => await _apiService.GetMinistryPerformanceAsync(),
                    "Sector" => await _apiService.GetSectorPerformanceAsync(),
                    "Donor" => await _apiService.GetDonorPerformanceAsync(),
                    _ => null
                };

                Items.Clear();
                if (data != null)
                    foreach (var item in data) Items.Add(item);
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
