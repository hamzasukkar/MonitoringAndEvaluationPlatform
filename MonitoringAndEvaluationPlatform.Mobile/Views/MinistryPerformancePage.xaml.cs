using MonitoringAndEvaluationPlatform.Mobile.Services;
using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class MinistryPerformancePage : ContentPage
    {
        private readonly CategoryPerformanceViewModel _viewModel;

        public MinistryPerformancePage(ApiService apiService)
        {
            InitializeComponent();
            _viewModel = new CategoryPerformanceViewModel(apiService, "Ministry");
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Items.Count == 0)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
