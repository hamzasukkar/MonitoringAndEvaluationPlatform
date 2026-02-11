using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class FrameworkPerformancePage : ContentPage
    {
        private readonly FrameworkPerformanceViewModel _viewModel;

        public FrameworkPerformancePage(FrameworkPerformanceViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Frameworks.Count == 0)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
