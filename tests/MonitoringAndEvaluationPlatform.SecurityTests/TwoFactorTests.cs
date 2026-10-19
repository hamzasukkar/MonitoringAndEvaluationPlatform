using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MonitoringAndEvaluationPlatform.Models;

namespace MonitoringAndEvaluationPlatform.SecurityTests;

/// <summary>
/// Authenticator (TOTP) enrolment.
///
/// The enrolment page builds an otpauth:// URI that the QR code encodes. It used to label
/// the account with the user's email address unconditionally, passing it straight to
/// UrlEncoder.Encode - which throws ArgumentNullException on null. Accounts are not required
/// to have an email, so for those users the page did not merely render a bad label: it
/// returned a 500 and two-factor enrolment was impossible.
/// </summary>
[Collection("security-app")]
public class TwoFactorTests
{
    private readonly SecurityWebApplicationFactory _factory;

    public TwoFactorTests(SecurityAppFixture fixture) => _factory = fixture.Factory;

    [Fact]
    public async Task EnrolmentUri_IsWellFormed_ForUserWithEmail()
    {
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var uri = await GetAuthenticatorUriAsync(client);

        AssertWellFormedOtpAuthUri(uri, expectedLabel: $"{SecurityWebApplicationFactory.AdminUserName}@example.test");
    }

    [Fact]
    public async Task EnrolmentUri_IsWellFormed_ForUserWithoutEmail()
    {
        const string userName = "test_no_email";

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            if (await userManager.FindByNameAsync(userName) is null)
            {
                var user = new ApplicationUser { UserName = userName, MustChangePassword = false };
                var created = await userManager.CreateAsync(user, SecurityWebApplicationFactory.TestPassword);
                Assert.True(created.Succeeded,
                    string.Join("; ", created.Errors.Select(e => e.Description)));
            }
        }

        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(userName, SecurityWebApplicationFactory.TestPassword);

        var uri = await GetAuthenticatorUriAsync(client);

        // The label must fall back to the user name, not collapse to empty.
        AssertWellFormedOtpAuthUri(uri, expectedLabel: userName);
    }

    [Fact]
    public async Task EnrolmentPage_LoadsQrCodeScripts()
    {
        // Without both scripts the page silently shows no QR code at all - only the manual
        // key - which is exactly the state this work removed.
        var client = _factory.CreateNonRedirectingClient();
        await client.LoginOrThrowAsync(
            SecurityWebApplicationFactory.AdminUserName, SecurityWebApplicationFactory.TestPassword);

        var html = await client.GetStringAsync("/Identity/Account/Manage/EnableAuthenticator");

        Assert.Contains("/lib/qrcodejs/qrcode.min.js", html);
        Assert.Contains("/js/qr-authenticator.js", html);
        Assert.Contains("id=\"qrCode\"", html);
    }

    private static async Task<string> GetAuthenticatorUriAsync(HttpClient client)
    {
        var response = await client.GetAsync("/Identity/Account/Manage/EnableAuthenticator");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(html, @"id=""qrCodeData""\s+data-url=""([^""]+)""");
        Assert.True(match.Success, "The page did not render a qrCodeData element with a data-url.");

        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static void AssertWellFormedOtpAuthUri(string uri, string expectedLabel)
    {
        Assert.StartsWith("otpauth://totp/", uri, StringComparison.Ordinal);

        // otpauth://totp/{issuer}:{label}?secret=...&issuer=...&digits=6
        var match = Regex.Match(uri, @"^otpauth://totp/(?<issuer>[^:]+):(?<label>[^?]+)\?(?<query>.+)$");
        Assert.True(match.Success, $"Malformed otpauth URI: {uri}");

        var issuer = Uri.UnescapeDataString(match.Groups["issuer"].Value);
        var label = Uri.UnescapeDataString(match.Groups["label"].Value);
        var query = match.Groups["query"].Value;

        Assert.False(string.IsNullOrWhiteSpace(label), "The account label must never be empty.");
        Assert.Equal(expectedLabel, label);

        // The scaffolding default was the literal "Microsoft.AspNetCore.Identity.UI".
        Assert.DoesNotContain("Identity.UI", issuer, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(issuer));

        Assert.Matches(@"secret=[A-Z2-7]+", query);
        Assert.Contains("digits=6", query);
    }
}
