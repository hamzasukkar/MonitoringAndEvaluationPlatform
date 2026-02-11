using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MonitoringAndEvaluationPlatform.Mobile.Models;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using System.Collections.ObjectModel;

namespace MonitoringAndEvaluationPlatform.Mobile.ViewModels
{
    public partial class OutcomeProgressViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public OutcomeProgressViewModel(ApiService apiService)
        {
            _apiService = apiService;
        }

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isRefreshing;

        public ObservableCollection<OutcomeProgress> Outcomes { get; } = new();

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                var outcomes = await _apiService.GetOutcomeProgressAsync();
                Outcomes.Clear();
                if (outcomes != null)
                    foreach (var o in outcomes) Outcomes.Add(o);
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
