using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class ProjectProgressPage : ContentPage
    {
        private readonly ProjectProgressViewModel _viewModel;

        public ProjectProgressPage(ProjectProgressViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Projects.Count == 0)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
