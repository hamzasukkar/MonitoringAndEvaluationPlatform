using MonitoringAndEvaluationPlatform.Mobile.ViewModels;

namespace MonitoringAndEvaluationPlatform.Mobile.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}
