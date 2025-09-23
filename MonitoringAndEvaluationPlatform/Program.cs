using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonitoringAndEvaluationPlatform.Data;
using MonitoringAndEvaluationPlatform.Infrastructure;
using MonitoringAndEvaluationPlatform.Models;
using MonitoringAndEvaluationPlatform.Services;

var builder = WebApplication.CreateBuilder(args);
 ApplicationDbContext applicationDbContext;

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
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
});

builder.Services.AddScoped<MonitoringService>();
builder.Services.AddScoped<PlanService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IProjectValidationService, ProjectValidationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
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

});

builder.Services.AddControllersWithViews()
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();


var app = builder.Build();



// Configure the HTTP request pipeline.
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
        await userManager.AddToRoleAsync(readingUser, UserRoles.ReadingUser);
    }
}

app.Run();
