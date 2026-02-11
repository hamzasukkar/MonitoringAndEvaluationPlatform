using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class CategoryPerformancePage : ContentPage
    {
        private readonly CategoryPerformanceViewModel _viewModel;

        public CategoryPerformancePage(CategoryPerformanceViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (_viewModel.Items.Count == 0)
                _viewModel.LoadDataCommand.Execute(null);
        }
    }
}
