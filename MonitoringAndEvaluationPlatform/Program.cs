using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    // Other options...
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

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
});

builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // Default login path
    options.AccessDeniedPath = "/Identity/Account/AccessDenied"; // If unauthorized
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure forwarded headers for reverse proxy (IIS, nginx, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IProjectValidationService, ProjectValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDataManagementService, DataManagementService>();
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
app.UseStaticFiles();

app.UseRouting();
var locOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
app.UseRequestLocalization(locOptions.Value);


app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (!context.User.Identity.IsAuthenticated && !context.Request.Path.StartsWithSegments("/Identity/Account/Login") && !context.Request.Path.StartsWithSegments("/Identity/Account/Register"))
    {
        context.Response.Redirect("/Identity/Account/Login");
        return;
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Frameworks}/{action=Index}/{id?}");
app.MapRazorPages();

// Create an admin role and user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.SeedAsync(services);
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

    // Create System Administrator user
    var sysAdminEmail = "admin@example.com";
    var sysAdminUser = await userManager.FindByEmailAsync(sysAdminEmail);
    if (sysAdminUser == null)
    {
        sysAdminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = sysAdminEmail,
            MinistryName = "System Administration"
        };
        await userManager.CreateAsync(sysAdminUser, "Admin@123");
    }

    // Ensure the user has the SystemAdministrator role (whether user was just created or already existed)
    if (!await userManager.IsInRoleAsync(sysAdminUser, UserRoles.SystemAdministrator))
    {
        await userManager.AddToRoleAsync(sysAdminUser, UserRoles.SystemAdministrator);
    }

    // Create Ministries User
    var ministriesEmail = "ministry@example.com";
    var ministriesUser = await userManager.FindByEmailAsync(ministriesEmail);
    if (ministriesUser == null)
    {
        ministriesUser = new ApplicationUser
        {
            UserName = "ministry_user",
            Email = ministriesEmail,
            MinistryName = "Ministry of Planning"
        };
        await userManager.CreateAsync(ministriesUser, "Ministry@123");
    }

    // Ensure the user has the MinistriesUser role
    if (!await userManager.IsInRoleAsync(ministriesUser, UserRoles.MinistriesUser))
    {
        await userManager.AddToRoleAsync(ministriesUser, UserRoles.MinistriesUser);
    }

    // Create Data Entry User
    var dataEntryEmail = "dataentry@example.com";
    var dataEntryUser = await userManager.FindByEmailAsync(dataEntryEmail);
    if (dataEntryUser == null)
    {
        dataEntryUser = new ApplicationUser
        {
            UserName = "data_entry",
            Email = dataEntryEmail,
            MinistryName = "Data Entry Department"
        };
        await userManager.CreateAsync(dataEntryUser, "DataEntry@123");
    }

    // Ensure the user has the DataEntry role
    if (!await userManager.IsInRoleAsync(dataEntryUser, UserRoles.DataEntry))
    {
        await userManager.AddToRoleAsync(dataEntryUser, UserRoles.DataEntry);
    }

    // Create Reading User
    var readingEmail = "reader@example.com";
    var readingUser = await userManager.FindByEmailAsync(readingEmail);
    if (readingUser == null)
    {
        readingUser = new ApplicationUser
        {
            UserName = "reading_user",
            Email = readingEmail,
            MinistryName = "External Observer"
        };
        await userManager.CreateAsync(readingUser, "Reader@123");
    }

    // Ensure the user has the ReadingUser role
    if (!await userManager.IsInRoleAsync(readingUser, UserRoles.ReadingUser))
    {
        await userManager.AddToRoleAsync(readingUser, UserRoles.ReadingUser);
    }
}

app.Run();
