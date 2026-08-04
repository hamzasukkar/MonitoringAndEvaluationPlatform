# Security decisions requiring your input

Open items from the 2026-08-04 security review. Each was deliberately **not** decided
during remediation. Nothing here is fixed — these are live gaps or pending choices.

---

## D1 — Leaked production database credentials (ACCEPTED RISK)

**Status: you chose to take no action.** Recorded here so the decision is explicit and can
be revisited.

Real credentials remain retrievable from the public GitHub repository:

```bash
# sql1004.site4now.net / db_abccd2_mre_admin / Sun@T@ime0ut@
git show ae83a07^:MonitoringAndEvaluationPlatform/appsettings.json

# also present in
git log --all -S 'site4now' --oneline -- '*appsettings.json'   # commits 33cbd21, ae83a07
```

A second live credential sits in build output on this machine (not in git, but staged for
FTP publish):

```
MonitoringAndEvaluationPlatform/bin/Release/net8.0/appsettings.json
MonitoringAndEvaluationPlatform/obj/Release/net8.0/PubTmp/Out/appsettings.json
  -> SQL1001.site4now.net / db_abea3f_mre_admin / Fares1993
```

The same password `Sun@T@ime0ut@` was reused across five database accounts.

**Why this was left alone:** you stated the platform is local-development only and chose
"do nothing". That is defensible while nothing production depends on those accounts.

**Revisit before any of these happen:**
- deploying to site4now (or anywhere) again
- adding a collaborator, or the repo being forked
- reusing any of those passwords on another system

**If you revisit, in order:** rotate the passwords first (that is what actually fixes it),
then decide about history. Rewriting history across 994 commits and ~25 branches
invalidates every clone, fork and open PR, and GitHub keeps unreachable objects addressable
by SHA until support-initiated GC — so rewriting is high-cost and not a guarantee.
Enabling GitHub secret scanning **push protection** is the cheap step that prevents
recurrence; the repository is public.

**What was done regardless:** `appsettings.json` now ships blank, the connection string
comes from user secrets or an environment variable, `.gitignore` covers the build-output
directories that were only incidentally ignored, and CI fails if a password reappears in a
tracked settings file.

---

## D2 — Which roles may enter measure and plan values (UNDECIDED)

**Status: you will choose later.** Until you do, `MeasuresController` and `PlansController`
are protected by authentication and ministry scoping, but **not** by role-specific
permissions.

### What is already enforced

- Class-level `[Authorize]` — anonymous access is closed.
- Ministry scoping on every action — a user of one ministry cannot read or modify another
  ministry's measures or plans. This holds under every candidate answer below, which is why
  it was applied without waiting.
- Global antiforgery validation.

### What is still open

`Infrastructure/PermissionMap.cs` currently maps the relevant permissions to **`DataEntry`
only**:

| Permission | Roles that hold it today |
|---|---|
| `AddMetricValue`, `EditMetricValues`, `DeleteMetricValues` | SystemAdministrator, DataEntry |
| `ModifyPlanStatus` | SystemAdministrator, DataEntry |
| `FillProjectForm` | SystemAdministrator, DataEntry |

If `[Permission(Permissions.AddMetricValue)]` were applied to `MeasuresController` today,
**`MinistriesUser` and `MinistryStrategyManager` would immediately lose measure entry.**
That is why it was not applied.

### The decision

Who should be able to enter measure and plan values?

**Option A — DataEntry only (matches the current map).** Apply the attributes as-is:

```csharp
// Controllers/MeasuresController.cs
[Permission(Permissions.ReadProjectMetrics)]   // on read actions
[Permission(Permissions.AddMetricValue)]       // AddMeasure, Create, CreateFromDetails
[Permission(Permissions.EditMetricValues)]     // Edit
[Permission(Permissions.DeleteMetricValues)]   // Delete, DeleteConfirmed

// Controllers/PlansController.cs
[Permission(Permissions.ReadActionPlans)]      // on read actions
[Permission(Permissions.ModifyPlanStatus)]     // UpdatePlanValue, UpdatePlanValues, Edit
```

**Option B — also MinistriesUser.** Same attributes, plus widen the map in
`Infrastructure/PermissionMap.cs`:

```csharp
Permissions.AddMetricValue or Permissions.EditMetricValues or Permissions.DeleteMetricValues =>
    userRoles.Contains(UserRoles.SystemAdministrator) ||
    userRoles.Contains(UserRoles.DataEntry) ||
    userRoles.Contains(UserRoles.MinistriesUser),      // <- add
```

**Option C — also MinistryStrategyManager.** As Option B, plus
`userRoles.Contains(UserRoles.MinistryStrategyManager)`.

**Option D — SystemAdministrator only.** Most restrictive; will block routine ministry data
entry. Not recommended unless entry is genuinely centralised.

Widen the **map**, not the attribute — the map is the single source of truth and
`PermissionMapTests` asserts the `/Admin/Roles` screen matches it.

---

## Additional items surfaced during remediation

These were fixed, but carry a decision or a follow-up you should be aware of.

### Every existing user must change their password at next sign-in
The `AddMustChangePassword` migration flags **all** existing accounts. This is intended —
85 seeded accounts shared publicly-known passwords (`Ministry@123`, `Admin@123`) — but it
will surprise users if not announced.

### Content-Security-Policy is report-only
`Infrastructure/SecurityHeadersMiddleware.cs` ships CSP in report-only mode. Several views
carry thousands of lines of inline `<script>` plus inline `onclick=` handlers, so an
enforcing policy would blank those pages. Set `Security:EnforceCsp` to `true` once inline
script has been moved out or nonces added. Until then CSP reports but does not block.

### Forwarded headers trust loopback only
`Program.cs` now trusts only loopback proxies. If you deploy behind a non-local reverse
proxy, set `ForwardedHeaders:KnownProxies` to its address — otherwise client IPs in the
audit log will show the proxy rather than the real client.

### Uploads location
New uploads are written outside the web root (`App_Data/uploads` by default, or
`Storage:UploadsRoot`). Files uploaded before this change remain physically in
`wwwroot/uploads`; they are no longer served statically (the pipeline 404s `/uploads`) and
are reachable only through `FilesController`. Moving them is optional cleanup.

### Client-side libraries not yet versioned
`wwwroot/js` still contains unpinned vendored copies of chart.js, select2, sweetalert2,
jstree and orgchart, and several layouts load CDN assets without Subresource Integrity.
See `MonitoringAndEvaluationPlatform/wwwroot/js/VENDOR.md`.

### Bootstrap upgrade needs UI verification
Local Bootstrap JS went 5.1.0 → 5.3.3 (5.1 carries a tooltip/popover sanitizer XSS). The
layouts were already loading Bootstrap 5.3 CSS from a CDN, so this removes a mismatch
rather than introducing one — but **modals, dropdowns, tooltips, tabs and collapse should
be clicked through** before this is trusted in production.
