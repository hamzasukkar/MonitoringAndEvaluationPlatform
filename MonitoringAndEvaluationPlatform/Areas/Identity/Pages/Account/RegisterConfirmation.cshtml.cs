// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MonitoringAndEvaluationPlatform.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Part of the closed self-registration flow (see RegisterModel). Kept as a 404 override for the
    /// same reason: deleting it would bring back the Identity UI's built-in page, which — with no
    /// email sender configured — prints an email-confirmation link for any registered address and
    /// reveals whether that address exists.
    /// </summary>
    public class RegisterConfirmationModel : PageModel
    {
        public IActionResult OnGet() => NotFound();
    }
}
