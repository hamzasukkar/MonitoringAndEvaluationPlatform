// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MonitoringAndEvaluationPlatform.Areas.Identity.Pages.Account
{
    /// <summary>
    /// Self-registration is closed. Accounts are created by an administrator (Admin → Users →
    /// Create User), who also assigns the ministry and role a user needs to see any data.
    ///
    /// The page itself must stay: deleting this scaffolded override would bring back the Identity
    /// UI's built-in Register page, which is open to anyone. So it answers 404 to every request,
    /// signed in or not.
    /// </summary>
    public class RegisterModel : PageModel
    {
        public IActionResult OnGet() => NotFound();

        public IActionResult OnPost() => NotFound();
    }
}
