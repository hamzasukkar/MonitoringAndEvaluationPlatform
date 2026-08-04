# Security

Findings from the 2026-08-04 review of this platform, what was done about each, and the
controls that now need to stay in place.

Open items that need **your** decision are in [SECURITY-DECISIONS.md](SECURITY-DECISIONS.md).

---

## The core problem

The application had **two authorization systems that never met**.

A sound permission-policy system (`PermissionAttribute` → `PermissionAuthorizationHandler`,
deny-by-default) protected some controllers. Ten others were guarded only by a hand-rolled
`app.Use(...)` middleware that ran *after* `UseAuthorization()`, enforced authentication but
never authorization, and explicitly whitelisted `/Identity/Account/Register`.

That produced an exploitable chain:

> open self-registration → account auto-signed-in with no role → full create/edit/delete on
> Measures, Plans, ProjectPhases, ActionPlans, Sectors, Donors, Location and **Ministries** —
> where `MinistriesController.Create` minted Identity users with the hardcoded password
> `Ministry@123`.

The middleware is gone, replaced by `AuthorizationOptions.FallbackPolicy`.

---

## Findings and status

### Critical

| # | Finding | Status |
|---|---|---|
| 1 | **Path traversal + arbitrary file overwrite.** `FrameworkGoalsController.UploadAttachment` built its target as `$"{Guid.NewGuid()}_{file.FileName}"` with no `Path.GetFileName` and opened it `FileMode.Create`. A multipart filename of `../../web.config` escaped the uploads folder and overwrote arbitrary files. | **Fixed** — all uploads go through `IUploadValidationService`; the stored name is entirely server-generated. |
| 2 | **Open self-registration.** Anyone could create an account, was signed in immediately, and reached ten unprotected controllers. | **Fixed** — `RegisterModel` is SystemAdministrator-only; the whitelist is gone. |
| 3 | **Hardcoded credentials seeded on every boot in every environment.** `admin`/`Admin@123` plus 81 ministry accounts all sharing `Ministry@123`, with predictable usernames. The admin role was re-granted on every start, undoing deliberate demotion. | **Fixed** — seeding is Development-only and opt-in, passwords come from configuration only, and the re-grant is removed. |
| 4 | **Cross-ministry IDOR.** Plans, Measures, ProjectPhases and ActionPlans were loaded by ID with no ownership check. Any authenticated user could rewrite any project's realised disbursement by incrementing `planCode`. | **Fixed** — `IMinistryScopeService` guards every such action. |

### High

| # | Finding | Status |
|---|---|---|
| 5 | **~47 state-changing actions had no CSRF protection**, including `Projects/DeleteConfirmed` and every `InlineDelete`. | **Fixed** — global `AutoValidateAntiforgeryTokenAttribute`. |
| 6 | **Uploads served anonymously.** `UseStaticFiles` runs before authentication, so every attachment under `wwwroot/uploads` was readable by URL, and an uploaded `.html`/`.svg` executed as same-origin script. | **Fixed** — uploads live outside the web root, `/uploads` 404s, `FilesController` serves them with authorization. |
| 7 | **No upload validation** on five paths — no extension, MIME, content or size checks. | **Fixed** — allow-list, size cap and magic-byte check. |
| 8 | **Stored XSS** via attachment filename concatenated into `innerHTML`. | **Fixed** — DOM built with `textContent`; filenames sanitized at the source. |
| 9 | **XSS via hand-rolled JSON** in `<script>` blocks (`ChartsView`, `Projects/Edit`). | **Fixed** — `Json.Serialize` throughout. |
| 10 | **Mass assignment of performance fields.** `Project` and `Indicator` Create bound `performance`/`DisbursementPerformance`, letting a caller post fabricated figures into the reporting hierarchy. | **Fixed** — zeroed after binding. |
| 11 | **No brute-force protection.** `lockoutOnFailure: false` at every call site meant the configured lockout never armed; no rate limiting; 6-character minimum password. | **Fixed** — lockout armed, IP-partitioned rate limiting, 12-character minimum. |
| 12 | **Debug endpoints in production.** `DebugController` had state-mutating and row-**deleting** `[HttpGet]` actions behind bare `[Authorize]` — CSRF-able with `<img src>`. | **Fixed** — controller deleted. |
| 13 | **Spoofable forwarded headers.** `KnownProxies.Clear()` let any client forge `X-Forwarded-For` (poisoning audit-log IPs, defeating the rate limiter) and `X-Forwarded-Proto` (defeating HTTPS redirection). | **Fixed** — loopback plus configured proxies only. |
| 14 | **Chatbot prompt injection.** The client-supplied message `role` was passed through verbatim, so a caller could inject their own `system` message and override the platform prompt. | **Fixed** — role clamped, history bounded. |
| 15 | **5 vulnerable transitive packages** (3 High) via an unused scaffolding package and an unused SQLite provider. | **Fixed** — both removed, stack updated to 8.0.20. Scan is clean. |

### Medium

| # | Finding | Status |
|---|---|---|
| 16 | **Permission map drift.** `RolePermissionService` held a second copy of the role→permission logic that had already diverged, so `/Admin/Roles` under-reported what `MinistryStrategyManager` could actually do. | **Fixed** — `PermissionMap` is the single source of truth; 360 tests assert the two agree. |
| 17 | **31 sites returned raw `ex.Message`** to clients, leaking SQL/EF table, column and constraint names. | **Fixed** — generic messages, exceptions logged. |
| 18 | **No security response headers** at all. | **Fixed** — see below. CSP is report-only. |
| 19 | **Unsanitized stored HTML** rendered raw in the guide history. | **Fixed** — `HtmlSanitizer` on write. |
| 20 | **Password reset unreachable.** `[AllowAnonymous]` was missing from the Identity pages, so ForgotPassword/ResetPassword redirected to login. | **Fixed.** |
| 21 | **Step-up re-auth was a password oracle.** `DataManagementController` re-checked the admin's password with `lockoutOnFailure: false` and logged only successes. | **Fixed** — lockout armed, failures audited. |
| 22 | **CSV formula injection** in the audit-log export into an admin's spreadsheet. | **Fixed** — leading `=+-@` neutralized. |
| 23 | **Data-protection keys not persisted** — auth cookies and reset tokens died on every app-pool recycle. | **Fixed.** |
| 24 | **Per-request PII logging.** The authorization handler logged username and full role set at Information on every check, into a persisted stdout log. | **Fixed** — demoted to Debug, structured. |
| 25 | **Four `ModelState.IsValid \|\| true` validation bypasses.** | **Fixed** — see note below. |

### Not vulnerabilities

- **SQL injection: none found.** Every `ExecuteSqlRaw*` call site interpolates only values
  from a hardcoded list or the connection string; the one user-influenced value is a real
  `SqlParameter`.
- **Open redirect: none found.** All `returnUrl` sinks use `LocalRedirect` or
  `Url.IsLocalUrl`.
- **Chatbot API key** is server-side only and its output is rendered with `.text()`.
- `includeIdentityData` on the backup script is accepted but never honoured, so password
  hashes were never exported. It is now explicitly documented as intentionally ignored.

---

## About the validation bypasses

`if (ModelState.IsValid || true)` appeared in four places. They were not laziness — they
masked a real defect. The login `InputModel` carried a `[Required] Email` property that the
login form never rendered and the handler never read, so `ModelState` could **never** be
valid. Removing the bypass without removing the dead property breaks sign-in entirely.

Both were fixed together, and `AuthenticationTests.ValidCredentials_SignIn` exists
specifically to catch a regression here.

---

## Controls that must stay in place

Adding a controller or an endpoint means honouring these. The test suite enforces each.

1. **`AuthorizationOptions.FallbackPolicy`** — every endpoint requires authentication unless
   it opts out with `[AllowAnonymous]`. Do not reintroduce a redirect middleware.
2. **`IMinistryScopeService`** — any action that loads a record by ID must gate it. Every
   method fails closed: no `MinistryCode` means access to nothing, never everything.
3. **`Infrastructure/PermissionMap.cs`** — the only role→permission mapping. A second copy
   is what caused finding 16.
4. **Global antiforgery** — `wwwroot/js/antiforgery.js` supplies the token for AJAX. It is
   loaded by all eight layouts. If a layout stops rendering the token, every AJAX write on
   pages using it breaks.
5. **`IUploadValidationService`** — all uploads. Never build a path from a client filename.
6. **`FilesController`** — the only way user-uploaded files are served.
7. **No secrets in `appsettings.json`** — user secrets in development, environment variables
   in production. CI fails the build if a password reappears in a tracked settings file.

---

## Response headers

Set by `Infrastructure/SecurityHeadersMiddleware.cs`, before `UseStaticFiles` so static
assets are covered:

| Header | Value |
|---|---|
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `Permissions-Policy` | `geolocation=(), camera=(), microphone=(), payment=(), usb=()` |
| `Content-Security-Policy-Report-Only` | see the middleware |

CSP is **report-only** deliberately — see SECURITY-DECISIONS.md.

---

## Running the security tests

```bash
dotnet test tests/MonitoringAndEvaluationPlatform.SecurityTests
```

416 tests. They boot the real pipeline via `WebApplicationFactory` against an in-memory
database, so middleware order, authorization policies and antiforgery are exercised as
assembled rather than in isolation.

| Area | What it locks down |
|---|---|
| `AuthenticationTests` | Anonymous access to 12 controllers; ForgotPassword reachable; Register closed; sign-in works; login POST needs a token |
| `MinistryScopeTests` | Cross-ministry read and write, asserted against the database as well as the status code; fail-closed scoping; admins not blocked |
| `CsrfTests` | 14 previously-unprotected POSTs rejected without a token, accepted with one; GET unaffected; layouts still render the token |
| `PermissionMapTests` | 360 role × permission pairs — enforcement and the `/Admin/Roles` display must agree |
| `UploadValidationServiceTests` | Traversal, content-type spoofing, oversize, display-name sanitisation, path containment |
| `SecurityHeaderTests` | Headers present; `/uploads` not served statically |
| `RateLimitingTests` | Login rate limiting actually triggers |

CI (`.github/workflows/ci.yml`) runs these plus a vulnerable-package gate — note that
`dotnet list package --vulnerable` exits 0 even on findings, so the workflow inspects its
output explicitly.

---

## Reporting a vulnerability

Report privately to the repository owner. Do not open a public issue.
