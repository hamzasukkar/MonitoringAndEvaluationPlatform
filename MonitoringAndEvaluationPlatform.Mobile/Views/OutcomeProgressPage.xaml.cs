using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class OutcomeProgressPage : ContentPage
    {
        private readonly OutcomeProgressViewModel _viewModel;

        public OutcomeProgressPage(OutcomeProgressViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Outcomes.Count == 0)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
