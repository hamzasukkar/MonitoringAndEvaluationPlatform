using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Infrastructure;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;
using QuestPDF.Infrastructure;

// Configure QuestPDF license (Community license for open-source projects)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
ApplicationDbContext applicationDbContext;

// Configure Kestrel to handle HTTP/2 properly behind IIS
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

// Add services to the container.
// Fail fast on a missing OR blank connection string. appsettings.json intentionally ships
// blank; supply it via user-secrets (dev) or the ConnectionStrings__DefaultConnection
// environment variable (prod). An empty string would otherwise fail later with a vague error.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not configured. Set it with " +
        "'dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"<conn>\"' for development, " +
        "or the ConnectionStrings__DefaultConnection environment variable in production.");
}

// Register HttpContextAccessor for AuditInterceptor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddScoped<TimestampInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(connectionString);
    // TimestampInterceptor must run before AuditInterceptor so audit logs capture the stamped values
    options.AddInterceptors(
        serviceProvider.GetRequiredService<TimestampInterceptor>(),
        serviceProvider.GetRequiredService<AuditInterceptor>());
});
// Development only: this filter renders full SQL/EF diagnostics (including the connection
// string) and an "Apply Migrations" button. It must never be registered in production.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

    // Password policy. Identity's default minimum is 6, which is too weak for a platform
    // holding ministry financial data. Existing password hashes are unaffected; this
    // applies only when a password is created or changed.
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;

    // Lockout. These defaults existed but were never armed because every call site passed
    // lockoutOnFailure: false - see Login.cshtml.cs and DataManagementController.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Validate the antiforgery token on every state-changing request (POST/PUT/PATCH/DELETE;
// GET/HEAD/OPTIONS/TRACE are skipped). Protection used to be opt-in per action via
// [ValidateAntiForgeryToken] and roughly 47 state-changing actions had opted out,
// including project deletion and every inline-delete endpoint.
// Client side, wwwroot/js/antiforgery.js supplies the token for AJAX callers.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Add Authorization policies for permissions
builder.Services.AddAuthorization(options =>
{
    // Register all permissions as policies
    var permissionFields = typeof(Permissions).GetFields();
    foreach (var field in permissionFields)
    {
        var permission = field.GetValue(null)?.ToString();
        if (!string.IsNullOrEmpty(permission))
        {
            options.AddPolicy(permission, policy =>
                policy.Requirements.Add(new PermissionRequirement(permission)));
        }
    }

    // Every endpoint requires an authenticated user unless it opts out with [AllowAnonymous].
    // This replaces a hand-rolled redirect middleware that ran *after* UseAuthorization(),
    // enforced authentication but never authorization, and whitelisted the Register page.
    // Unlike that middleware this honours [AllowAnonymous] and returns a proper 401/403.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // Default login path
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // If unauthorized
    options.Cookie.HttpOnly = true;
    // Always outside Development so the auth cookie is never sent over plaintext HTTP.
    // Development keeps SameAsRequest because the local "http" launch profile would
    // otherwise never receive a cookie at all.
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    // Lax, not Strict: Strict breaks the post-login returnUrl navigation.
    options.Cookie.SameSite = SameSiteMode.Lax;
    // Previously unset, which meant a 14-day sliding session.
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Configure forwarded headers for reverse proxy (IIS, nginx, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    // Only trust forwarded headers from known proxies. Clearing both lists (the previous
    // behaviour) let ANY client forge X-Forwarded-For - spoofing the IP recorded in the
    // audit log and handing out a fresh rate-limit bucket per request - and forge
    // X-Forwarded-Proto: https to defeat UseHttpsRedirection.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // IIS forwards from the local machine.
    options.KnownProxies.Add(IPAddress.Loopback);
    options.KnownProxies.Add(IPAddress.IPv6Loopback);

    // Additional proxies (comma-separated) for deployments behind a non-local reverse proxy.
    var extraProxies = builder.Configuration["ForwardedHeaders:KnownProxies"];
    if (!string.IsNullOrWhiteSpace(extraProxies))
    {
        foreach (var proxy in extraProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    }
});

// Rate limiting. There was previously no brute-force protection of any kind on login.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Credential-guessing protection, partitioned by client IP. Only meaningful because
    // KnownProxies is now restricted above - otherwise a spoofed X-Forwarded-For would
    // create a new partition on every request.
    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5)
            }));

    // Metered third-party LLM calls - protects the API quota/bill.
    options.AddPolicy(RateLimitPolicies.Chatbot, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Destructive admin operations behind step-up re-authentication.
    options.AddPolicy(RateLimitPolicies.Sensitive, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5)
            }));
});

builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IProjectValidationService, ProjectValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDataManagementService, DataManagementService>();
builder.Services.AddScoped<IMinistryScopeService, MinistryScopeService>();
builder.Services.AddScoped<IUploadValidationService, UploadValidationService>();

// Persist data-protection keys. Without this they live in the app-pool profile and are
// regenerated on recycle, which silently invalidates every auth cookie and every
// password-reset token. Keep the path outside wwwroot and outside the deploy folder.
var dataProtectionKeyPath = builder.Configuration["DataProtection:KeyPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyPath))
{
    Directory.CreateDirectory(dataProtectionKeyPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyPath))
        .SetApplicationName("MonitoringAndEvaluationPlatform");
}
else
{
    builder.Services.AddDataProtection()
        .SetApplicationName("MonitoringAndEvaluationPlatform");
}
builder.Services.AddScoped<IGuideService, GuideService>();
// Lets the guide editor send the anti-forgery token on JSON POSTs via header.
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddScoped<MonitoringAndEvaluationPlatform.Helpers.INavigationHelper, MonitoringAndEvaluationPlatform.Helpers.NavigationHelper>();
builder.Services.Configure<ChatbotSettings>(builder.Configuration.GetSection("Chatbot"));
builder.Services.AddHttpClient();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ar"), // Arabic
        new CultureInfo("fr")  // French, optional
    };

    options.DefaultRequestCulture = new RequestCulture("ar");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Prioritize Arabic by removing the Accept-Language header provider
    // This ensures the DefaultRequestCulture ("ar") is used unless a specific culture is requested via QueryString or Cookie
    options.RequestCultureProviders.Clear();
    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());

});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();


var app = builder.Build();



// Configure the HTTP request pipeline.
// Use forwarded headers first (important for reverse proxy/IIS)
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Before UseStaticFiles so the headers cover static assets too.
app.UseSecurityHeaders();

// User uploads must never be served as static files. UseStaticFiles runs before
// UseAuthentication, so anything under wwwroot is anonymous - which meant every project,
// request, measure and goal attachment was readable by URL, and an uploaded .html or .svg
// executed as same-origin script. New uploads live outside wwwroot; this 404 also covers
// files written to wwwroot/uploads before that change. They stay reachable through the
// authorized download actions only.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    await next();
});

app.UseStaticFiles();

app.UseRouting();
var locOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Must run after authentication so the flagged user is known, and after authorization
// so [AllowAnonymous] endpoints are not forced through the change-password gate.
app.UsePasswordChangeEnforcement();

// NOTE: the hand-rolled "redirect anonymous users to Login" middleware that used to sit
// here has been removed. It ran after UseAuthorization(), enforced authentication but never
// authorization, whitelisted /Identity/Account/Register (open self-registration), and blocked
// the password-reset pages for anonymous users. It is replaced by AuthorizationOptions
// .FallbackPolicy above, which runs inside UseAuthorization() and honours [AllowAnonymous].

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Frameworks}/{action=Index}/{id?}");
// Rate limiting for the sign-in path is applied per-page via [EnableRateLimiting] on
// LoginModel rather than to every Razor Page.
app.MapRazorPages();

// Create an admin role and user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Demo accounts are Development-only and opt-in. Reference data always seeds.
    var seedDemoUsers = app.Environment.IsDevelopment()
        && builder.Configuration.GetValue<bool>("Seeding:EnableDemoUsers");

    await DbInitializer.SeedAsync(services, seedDemoUsers);
    ApplicationDbInitializer.SeedGovernoratesFromJson(dbContext);
    ApplicationDbInitializer.SeedDistrictsFromJson(dbContext);
    ApplicationDbInitializer.SeedSubDistrictsFromJson(dbContext);
    ApplicationDbInitializer.SeedCommunitiesFromJson(dbContext);
    ApplicationDbInitializer.EnsureGovernorateLocationChains(dbContext);

    // Create roles if they don't exist
    var rolesToCreate = new[]
    {
        UserRoles.SystemAdministrator,
        UserRoles.MinistriesUser,
        UserRoles.DataEntry,
        UserRoles.ReadingUser
    };

    foreach (var role in rolesToCreate)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Demo accounts. These previously ran unconditionally in EVERY environment with
    // hardcoded passwords, putting a known-credential SystemAdministrator on production,
    // and re-granted the admin role on every boot (undoing a deliberate demotion).
    // Now: Development only, opt-in, and passwords must come from configuration
    // (user-secrets) - there is no literal fallback.
    if (seedDemoUsers)
    {
        var seedLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DemoUserSeeding");

        async Task SeedDemoUserAsync(string configKey, string userName, string email, string ministryName, string role)
        {
            var password = builder.Configuration[$"Seeding:{configKey}"];
            if (string.IsNullOrWhiteSpace(password))
            {
                seedLogger.LogWarning(
                    "Skipping demo user {UserName}: no password configured at Seeding:{ConfigKey}.",
                    userName, configKey);
                return;
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    MinistryName = ministryName,
                    MustChangePassword = true
                };
                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    seedLogger.LogWarning("Could not create demo user {UserName}: {Errors}",
                        userName, string.Join("; ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                await userManager.AddToRoleAsync(user, role);
            }
        }

        await SeedDemoUserAsync("AdminPassword", "admin", "admin@example.com", "System Administration", UserRoles.SystemAdministrator);
        await SeedDemoUserAsync("MinistryPassword", "ministry_user", "ministry@example.com", "Ministry of Planning", UserRoles.MinistriesUser);
        await SeedDemoUserAsync("DataEntryPassword", "data_entry", "dataentry@example.com", "Data Entry Department", UserRoles.DataEntry);
        await SeedDemoUserAsync("ReaderPassword", "reading_user", "reader@example.com", "External Observer", UserRoles.ReadingUser);
    }
}

app.Run();
