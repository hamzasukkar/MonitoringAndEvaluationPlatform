using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class ProjectProgressViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public ProjectProgressViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        public ObservableCollection<ProjectProgress> Projects { get; } = new();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var projects = await _apiService.GetProjectProgressAsync();
                Projects.Clear();
                if (projects != null)
                    foreach (var p in projects) Projects.Add(p);
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
