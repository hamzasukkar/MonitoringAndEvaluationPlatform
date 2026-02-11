using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class ReportsPage : ContentPage
    {
        private readonly ReportsViewModel _viewModel;

        public ReportsPage(ReportsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Summary == null)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
