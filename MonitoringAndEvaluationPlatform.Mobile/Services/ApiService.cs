using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MonitoringAndEvaluationPlatform.Mobile.Models;

namespace MonitoringAndEvaluationPlatform.Mobile.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthService _authService;
        private readonly SettingsService _settingsService;

        public ApiService(AuthService authService, SettingsService settingsService)
        {
            _authService = authService;
            _settingsService = settingsService;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler);
        }

        private string BaseUrl => _settingsService.ServerUrl.TrimEnd('/');

        private async Task SetAuthHeaderAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private void SetLanguageHeader()
        {
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Clear();
            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(
                new StringWithQualityHeaderValue(_settingsService.Language));
        }

        private async Task<T?> GetAsync<T>(string endpoint)
        {
            await SetAuthHeaderAsync();
            SetLanguageHeader();

            var response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (await TryRefreshTokenAsync())
                {
                    await SetAuthHeaderAsync();
                    response = await _httpClient.GetAsync($"{BaseUrl}/{endpoint}");
                }
                else
                {
                    _authService.Logout();
                    return default;
                }
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            SetLanguageHeader();

            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/{endpoint}", data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        private async Task<bool> TryRefreshTokenAsync()
        {
            var token = await _authService.GetTokenAsync();
            var refreshToken = await _authService.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                return false;

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/api/auth/refresh",
                    new RefreshTokenRequest { Token = token, RefreshToken = refreshToken });

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result == null) return false;

                await _authService.SaveTokensAsync(result);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Auth
        public async Task<LoginResponse?> LoginAsync(string email, string password)
        {
            var result = await PostAsync<LoginRequest, LoginResponse>("api/auth/login",
                new LoginRequest { Email = email, Password = password });

            if (result != null)
                await _authService.SaveTokensAsync(result);

            return result;
        }

        public async Task<UserProfile?> GetProfileAsync() =>
            await GetAsync<UserProfile>("api/auth/profile");

        // Dashboard
        public async Task<DashboardSummary?> GetDashboardSummaryAsync() =>
            await GetAsync<DashboardSummary>("api/dashboard/summary");

        public async Task<List<FrameworkPerformance>?> GetFrameworksPerformanceAsync(int? frameworkCode = null, int? ministryCode = null)
        {
            var query = "api/dashboard/frameworks-performance";
            var parameters = new List<string>();
            if (frameworkCode.HasValue) parameters.Add($"frameworkCode={frameworkCode}");
            if (ministryCode.HasValue) parameters.Add($"ministryCode={ministryCode}");
            if (parameters.Any()) query += "?" + string.Join("&", parameters);
            return await GetAsync<List<FrameworkPerformance>>(query);
        }

        public async Task<List<OutcomeProgress>?> GetOutcomeProgressAsync(int? frameworkCode = null)
        {
            var query = frameworkCode.HasValue
                ? $"api/dashboard/outcome-progress?frameworkCode={frameworkCode}"
                : "api/dashboard/outcome-progress";
            return await GetAsync<List<OutcomeProgress>>(query);
        }

        public async Task<List<ProjectProgress>?> GetProjectProgressAsync(int? sectorId = null, int? donorId = null)
        {
            var query = "api/dashboard/project-progress";
            var parameters = new List<string>();
            if (sectorId.HasValue) parameters.Add($"sectorId={sectorId}");
            if (donorId.HasValue) parameters.Add($"donorId={donorId}");
            if (parameters.Any()) query += "?" + string.Join("&", parameters);
            return await GetAsync<List<ProjectProgress>>(query);
        }

        // Reports
        public async Task<ReportsSummary?> GetReportsSummaryAsync() =>
            await GetAsync<ReportsSummary>("api/reports/summary");

        public async Task<TopBottomPerformers?> GetTopPerformersAsync(int count = 5) =>
            await GetAsync<TopBottomPerformers>($"api/reports/top-performers?count={count}");

        public async Task<TopBottomPerformers?> GetBottomPerformersAsync(int count = 5) =>
            await GetAsync<TopBottomPerformers>($"api/reports/bottom-performers?count={count}");

        public async Task<List<CategoryReport>?> GetMinistryPerformanceAsync() =>
            await GetAsync<List<CategoryReport>>("api/reports/ministry-performance");

        public async Task<List<CategoryReport>?> GetSectorPerformanceAsync() =>
            await GetAsync<List<CategoryReport>>("api/reports/sector-performance");

        public async Task<List<CategoryReport>?> GetDonorPerformanceAsync() =>
            await GetAsync<List<CategoryReport>>("api/reports/donor-performance");

        public async Task<BudgetOverview?> GetBudgetOverviewAsync() =>
            await GetAsync<BudgetOverview>("api/reports/budget-overview");

        public async Task<PerformanceDistribution?> GetPerformanceDistributionAsync() =>
            await GetAsync<PerformanceDistribution>("api/reports/performance-distribution");

        // Lookups
        public async Task<List<LookupItemInt>?> GetMinistriesAsync() =>
            await GetAsync<List<LookupItemInt>>("api/lookups/ministries");

        public async Task<List<LookupItemInt>?> GetSectorsAsync() =>
            await GetAsync<List<LookupItemInt>>("api/lookups/sectors");

        public async Task<List<LookupItemInt>?> GetDonorsAsync() =>
            await GetAsync<List<LookupItemInt>>("api/lookups/donors");

        public async Task<List<LookupItem>?> GetGovernoratesAsync() =>
            await GetAsync<List<LookupItem>>("api/lookups/governorates");
    }
}
