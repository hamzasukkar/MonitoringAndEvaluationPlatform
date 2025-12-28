// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

namespace MonitoringAndEvaluationPlatform.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IAuditService _auditService;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ILogger<LogoutModel> logger, IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            // Get user info before signing out
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;
            var userName = user?.UserName;
            var email = user?.Email;

            await _signInManager.SignOutAsync();

            // Log the logout event
            await _auditService.LogAuthenticationAsync("Logout", userId, userName, email, true);

            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
