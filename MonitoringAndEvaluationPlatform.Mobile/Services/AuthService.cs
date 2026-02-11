using MonitoringAndEvaluationPlatform.Mobile.Models;

namespace MonitoringAndEvaluationPlatform.Mobile.Services
{
    public class AuthService
    {
        private const string TokenKey = "auth_token";
        private const string RefreshTokenKey = "refresh_token";
        private const string TokenExpiryKey = "token_expiry";

        public async Task SaveTokensAsync(LoginResponse response)
        {
            await SecureStorage.SetAsync(TokenKey, response.Token);
            await SecureStorage.SetAsync(RefreshTokenKey, response.RefreshToken);
            await SecureStorage.SetAsync(TokenExpiryKey, response.Expiration.ToString("O"));
        }

        public async Task<string?> GetTokenAsync()
        {
            return await SecureStorage.GetAsync(TokenKey);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await SecureStorage.GetAsync(RefreshTokenKey);
        }

        public async Task<bool> IsTokenExpiredAsync()
        {
            var expiryStr = await SecureStorage.GetAsync(TokenExpiryKey);
            if (string.IsNullOrEmpty(expiryStr)) return true;

            if (DateTime.TryParse(expiryStr, out var expiry))
                return DateTime.UtcNow >= expiry.AddMinutes(-2); // 2-minute buffer

            return true;
        }

        public bool IsLoggedIn => SecureStorage.GetAsync(TokenKey).Result != null;

        public void Logout()
        {
            SecureStorage.Remove(TokenKey);
            SecureStorage.Remove(RefreshTokenKey);
            SecureStorage.Remove(TokenExpiryKey);
        }
    }
}
