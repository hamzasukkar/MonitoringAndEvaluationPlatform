using System.Net;
using System.Text.RegularExpressions;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Helpers for driving the real login form and the real antiforgery token, so the tests
/// exercise the same path a browser does.
/// </summary>
public static class TestClientExtensions
{
    private static readonly Regex TokenRegex = new(
        @"name=""__RequestVerificationToken""[^>]*value=""(?<token>[^""]+)""",
        RegexOptions.Compiled);

    /// <summary>Extracts the antiforgery token rendered on a page.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string path)
    {
        var html = await client.GetStringAsync(path);
        var match = TokenRegex.Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException($"No antiforgery token found on {path}.");
        }

        return match.Groups["token"].Value;
    }

    /// <summary>
    /// Signs in through the real login form. Returns the response so callers can assert on
    /// failure cases (lockout, rate limiting, invalid credentials).
    /// </summary>
    public static async Task<HttpResponseMessage> LoginAsync(
        this HttpClient client, string userName, string password)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Identity/Account/Login");

        return await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.UserName"] = userName,
                ["Input.Password"] = password,
                ["Input.RememberMe"] = "false",
                ["__RequestVerificationToken"] = token
            }));
    }

    /// <summary>Signs in and asserts it worked, for tests whose subject is not login itself.</summary>
    public static async Task LoginOrThrowAsync(this HttpClient client, string userName, string password)
    {
        var response = await client.LoginAsync(userName, password);
        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException(
                $"Login for {userName} failed with {(int)response.StatusCode}; expected a redirect.");
        }
    }

    /// <summary>POSTs form values along with a valid antiforgery token.</summary>
    public static async Task<HttpResponseMessage> PostWithTokenAsync(
        this HttpClient client, string path, IDictionary<string, string> form, string tokenPage = "/Projects")
    {
        var token = await client.GetAntiforgeryTokenAsync(tokenPage);
        var content = new FormUrlEncodedContent(form);
        content.Headers.Add("RequestVerificationToken", token);
        return await client.PostAsync(path, content);
    }
}
