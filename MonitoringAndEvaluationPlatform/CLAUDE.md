# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build and Run
- `dotnet run` - Run the application (https://localhost:7173)
- `dotnet build` - Build the project
- `dotnet publish` - Publish for deployment

### Database Management
- `dotnet ef database update` - Apply pending migrations
- `dotnet ef migrations add <MigrationName>` - Create a new migration
- `dotnet ef database drop` - Drop the database (use with caution)

### Configuration
The connection string is **not** stored in `appsettings.json` — that file has leaked
credentials before and now ships blank.

- Development: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<conn>"`
- Production: set the `ConnectionStrings__DefaultConnection` environment variable

Demo accounts are Development-only and opt-in. Set `Seeding:EnableDemoUsers` to `true` in
`appsettings.Development.json` and supply each password via user secrets
(`Seeding:AdminPassword`, `Seeding:MinistryPassword`, `Seeding:DataEntryPassword`,
`Seeding:ReaderPassword`). Accounts without a configured password are skipped. There are no
hardcoded default credentials.

Every account is created with `MustChangePassword` set, so the first sign-in is redirected
to the change-password page.

### Security tests
- `dotnet test tests/MonitoringAndEvaluationPlatform.SecurityTests` — authorization, CSRF,
  cross-ministry IDOR, upload validation, headers and rate limiting. See `SECURITY.md`.

## Architecture Overview

ASP.NET Core 8 MVC application for monitoring and evaluating development projects with a hierarchical performance tracking system.

### Domain Hierarchy

```
Framework → Outcome → Output → SubOutput → Indicator → Project → ActionPlan → Plan
```

Key entities:
- **Framework** – Top-level monitoring framework (e.g., SDGs)
- **Outcome / Output / SubOutput** – Hierarchical results chain
- **Indicator** – Measurable metrics with weights; linked to projects via `ProjectIndicator`
- **Project** – Implementation units linked to Ministry, Sector, Donor, Supervisor, and geographic locations
- **Measure** – Joins projects to indicators with planned/realized values
- **ActionPlan / Plan** – One ActionPlan per ProjectPhase; Plans are monthly entries with a `Realised` value used for disbursement performance
- **FrameworkGoal / FrameworkGoalYearlyValue** – Strategic goal tracking with yearly values and file attachments

### Location Hierarchy
`Governorate → District → SubDistrict → Community`

### Many-to-Many Relationships
- `Project ↔ Sector` (ProjectSectors)
- `Project ↔ Ministry` (ProjectMinistries)
- `Project ↔ Indicator` (ProjectIndicator — includes extra properties)
- `Project ↔ Donor` (ProjectDonor — includes FundingPercentage, FundingAmount)
- `Project ↔ Governorate/District/SubDistrict/Community`

---

## Services

| Service | Interface | Purpose |
|---------|-----------|---------|
| `PlanService` | — | Updates plans, cascades performance up the hierarchy via `UpdateProjectPerformance()` |
| `MonitoringService` | — | Monitoring data management and performance calculations |
| `PerformanceService` | `IPerformanceService` | Weighted-average performance aggregation (Project → Indicator → SubOutput → Output → Outcome → Framework) |
| `AuditService` | `IAuditService` | Audit log writes; backed by `AuditInterceptor` (EF Core interceptor) |
| `DataManagementService` | `IDataManagementService` | Export (Excel/PDF), backup, data deletion, value reset |
| `ChatbotService` | `IChatbotService` | Groq LLM API integration (model: `llama-3.3-70b-versatile`) |
| `NotificationService` | `INotificationService` | In-app user notifications |
| `ProjectValidationService` | `IProjectValidationService` | Project-level validation rules |
| `RolePermissionService` | — | Role-to-permission resolution |
| `NavigationHelper` | `INavigationHelper` | Breadcrumb and navigation path generation |

---

## Controllers

**Hierarchy:** FrameworksController, OutcomesController, OutputsController, SubOutputsController, IndicatorsController
**Projects:** ProjectsController, MeasuresController, ActionPlansController, PlansController
**Lookups:** MinistriesController, SectorsController, DonorsController, ProjectManagersController, SuperVisorsController, LocationController
**Reporting / Monitoring:** DashboardController, MonitoringController, ReportsController, TreeController
**Strategic:** FrameworkGoalsController
**Admin:** AdminController, AuditLogsController, DataManagementController
**Other:** ChatbotController, HomeController, FilesController (authorized attachment access)

---

## Authorization & Roles

Four built-in roles defined in `Models/UserRoles.cs`:
- `SystemAdministrator`
- `MinistriesUser`
- `DataEntry`
- `ReadingUser`

Permissions are defined as string constants in `Models/Permissions.cs` (80+ entries).
Custom policy enforcement via `Infrastructure/PermissionAuthorizationHandler.cs`.
Policies are registered in `Program.cs` per permission constant.

---

## Data & Persistence

- **ORM:** Entity Framework Core 8 with SQL Server (SQLite available as alternative)
- **Context:** `Data/ApplicationDbContext.cs` — Identity + all domain entities
- **Audit logging:** `AuditInterceptor` intercepts `SaveChanges` and writes to `AuditLog` table (user, timestamp, entity, operation, old/new values)
- **Seed data:** `Infrastructure/DbInitializer.cs` runs on startup — creates roles, 4 default users, and loads location hierarchy from JSON files in `SeedData/`
- **Migrations:** 22 migrations under `Migrations/` (latest adds exchange rate and currency fields to Project)

---

## Views & Layouts

Suffix-based localization: views look up `.ar.resx` / `.en.resx` / `.fr.resx` resource files.

**Shared layouts:**
`_Layout`, `_DashboardLayout`, `_DashboardHomeLayout`, `_ProjectsLayout`, `_MonitoringLayout`, `_ResultsFrameworkLayout`, `_AssessmentFrameworkLayout`, `_SetUpLayout`, `_ManagementNavigation`

**Key partials:**
`_HierarchyBreadcrumb`, `_Notifications`, `_ProgressBar`, `_ChatbotWidget`, `_HelpModal`, `_BackButton`, `_NavThemeDropdown`

**Helpers:** `ProgressHelper`, `ProgressBarHelper`, `ProgressTagHelper` — render performance indicators in views.

---

## ViewModels

Located in `ViewModels/`:
- User/role management: `CreateUserViewModel`, `EditUserViewModel`, `UserManagementViewModel`, `RoleViewModel`, `RoleManagementViewModel`, `UserViewModel`
- Audit: `AuditLogViewModel`
- Data management: `BackupViewModel`, `ClearDataViewModel`, `DeleteProjectViewModel`, `ResetValuesViewModel`, `SecurityConfirmationViewModel` (security confirmation required for destructive ops)

---

## NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.11 | Identity with EF Core |
| `Microsoft.EntityFrameworkCore.SqlServer` 8.0.11 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Sqlite` 8.0.11 | SQLite alternative |
| `Microsoft.EntityFrameworkCore.Tools` 8.0.11 | Migrations CLI |
| `ClosedXML` 0.104.2 | Excel export/import |
| `QuestPDF` 2024.12.2 | PDF generation (Community license — set in `Program.cs`) |

---

## Localization

- Supports Arabic (default), English, French
- Culture set via request localization middleware
- Resource files under `Resources/Views/` and `Resources/Models/`
- `RequestLocalizationOptions` configured in `Program.cs`

---

## External Integrations

- **Groq API** – LLM-powered chatbot (`ChatbotService`); API key and model configured in `appsettings.json` under `ChatbotSettings`
- **QuestPDF** – Report PDF generation in `ReportsController`
- **ClosedXML** – Excel export in `DataManagementController`

---

## Key Patterns & Conventions

- **Service layer** – All business logic in `Services/`; controllers are thin
- **Tests** – `tests/MonitoringAndEvaluationPlatform.SecurityTests` holds the security
  regression suite (authorization, CSRF, IDOR, uploads, headers). There is still no
  functional/unit test coverage of business logic. `DebugController` and `TestController`
  were removed; do not reintroduce debug endpoints as a testing mechanism.
- **Ministry scoping** – any action that loads a record by ID must gate it with
  `IMinistryScopeService`; every method there fails closed
- **Uploads** – always go through `IUploadValidationService`, which stores files outside
  `wwwroot` under a server-generated name. Serve them only via `FilesController`
- **Permissions** – `Infrastructure/PermissionMap.cs` is the single source of truth; do not
  add a second copy of the role→permission mapping
- **Performance propagation** – Always update via `PlanService.UpdateProjectPerformance()` to keep hierarchy metrics consistent; never update performance fields directly
- **Permissions** – Use the `Permissions` constants when adding new `[Authorize(Policy = ...)]` attributes; register the policy in `Program.cs`
- **Migrations** – Run `dotnet ef migrations add <Name>` then `dotnet ef database update` after any model change; never edit existing migration files
- **Seed data** – Location JSON files are in `SeedData/`; loaded once on startup if table is empty
