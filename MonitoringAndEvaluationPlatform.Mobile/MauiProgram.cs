using CommunityToolkit.Maui;
using Microcharts.Maui;
using MonitoringAndEvaluationPlatform.Mobile.Services;
using MonitoringAndEvaluationPlatform.Mobile.ViewModels;
using MonitoringAndEvaluationPlatform.Mobile.Views;

namespace MonitoringAndEvaluationPlatform.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<SettingsService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<ApiService>();

            // ViewModels
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<FrameworkPerformanceViewModel>();
            builder.Services.AddTransient<OutcomeProgressViewModel>();
            builder.Services.AddTransient<ProjectProgressViewModel>();
            builder.Services.AddTransient<ReportsViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<FrameworkPerformancePage>();
            builder.Services.AddTransient<OutcomeProgressPage>();
            builder.Services.AddTransient<ProjectProgressPage>();
            builder.Services.AddTransient<ReportsPage>();
            builder.Services.AddTransient<MinistryPerformancePage>();
            builder.Services.AddTransient<SectorPerformancePage>();
            builder.Services.AddTransient<DonorPerformancePage>();
            builder.Services.AddTransient<SettingsPage>();

            return builder.Build();
        }
    }
}
