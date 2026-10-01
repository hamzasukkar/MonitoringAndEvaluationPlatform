using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <summary>
    /// Makes Projects.MinistryCode the single owner of a project and repairs the data behind
    /// ministry isolation. The model does not change; this is data only.
    ///
    ///   A. Projects.MinistryCode is back-filled where it is NULL and the project has exactly ONE
    ///      ProjectMinistries row. With 2+ rows (ambiguous) or none (orphan) it stays NULL, which
    ///      makes the project visible to administrators only until one assigns an owner — never
    ///      an automatic pick, which could hand a project to the wrong ministry.
    ///   B. ProjectMinistries is re-synced to exactly { MinistryCode } for every owned project, so
    ///      no page can name a second ministry against it.
    ///   C. AspNetUsers.MinistryCode is back-filled where it is NULL and the free-text MinistryName
    ///      matches exactly one ministry (English name, Arabic name or MinistryUserName).
    ///      SystemAdministrators are skipped: they are never scoped.
    ///
    /// A non-NULL MinistryCode is never overwritten. Raw SQL bypasses the AuditInterceptor, so every
    /// changed row is written to dbo.MinistryOwnershipMigrationLog (deliberately not in the EF
    /// model); Down() replays it.
    ///
    /// Run Scripts/MinistryOwnership_PreFlight.sql first (and keep its output), then
    /// Scripts/MinistryOwnership_PostCheck.sql, then Data Management → "Recalculate Ministry
    /// Performance" so the stored ministry figures move onto the owned-project basis.
    /// </summary>
    public partial class BackfillMinistryOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AspNetUsers carries a filtered index, and SQL Server refuses DML on such a table
            // unless QUOTED_IDENTIFIER is ON. SqlClient turns it on, but sqlcmd does not, so a
            // 'migrations script' output run through sqlcmd would otherwise fail (and roll back).
            migrationBuilder.Sql("SET QUOTED_IDENTIFIER ON;");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[MinistryOwnershipMigrationLog]', N'U') IS NULL
CREATE TABLE [dbo].[MinistryOwnershipMigrationLog] (
    [Id]           int IDENTITY(1,1) NOT NULL CONSTRAINT [PK_MinistryOwnershipMigrationLog] PRIMARY KEY,
    [Action]       nvarchar(40)  NOT NULL,
    [ProjectID]    int           NULL,
    [MinistryCode] int           NULL,
    [UserId]       nvarchar(450) NULL,
    [Detail]       nvarchar(400) NULL,
    [AppliedAtUtc] datetime2     NOT NULL
        CONSTRAINT [DF_MinistryOwnershipMigrationLog_AppliedAtUtc] DEFAULT (SYSUTCDATETIME())
);");

            migrationBuilder.Sql(@"
SET NOCOUNT ON;

-- A. Owner back-fill: NULL owner and exactly one mirror row.
UPDATE p
SET    p.[MinistryCode] = sole.[MinistriesCode]
OUTPUT N'ProjectOwnerBackfill', inserted.[ProjectID], inserted.[MinistryCode], N'sole ProjectMinistries row'
INTO   [dbo].[MinistryOwnershipMigrationLog] ([Action], [ProjectID], [MinistryCode], [Detail])
FROM   [Projects] p
INNER JOIN (SELECT [ProjectsProjectID], MIN([MinistriesCode]) AS [MinistriesCode]
            FROM [ProjectMinistries]
            GROUP BY [ProjectsProjectID]
            HAVING COUNT(*) = 1) sole
        ON sole.[ProjectsProjectID] = p.[ProjectID]
WHERE  p.[MinistryCode] IS NULL;

-- B1. Remove mirror rows that name a ministry other than the owner.
DELETE pm
OUTPUT N'ProjectMinistryLinkRemoved', deleted.[ProjectsProjectID], deleted.[MinistriesCode], N'stale mirror row'
INTO   [dbo].[MinistryOwnershipMigrationLog] ([Action], [ProjectID], [MinistryCode], [Detail])
FROM   [ProjectMinistries] pm
INNER JOIN [Projects] p ON p.[ProjectID] = pm.[ProjectsProjectID]
WHERE  p.[MinistryCode] IS NOT NULL
  AND  pm.[MinistriesCode] <> p.[MinistryCode];

-- B2. Add the owner's mirror row where it is missing.
INSERT INTO [ProjectMinistries] ([MinistriesCode], [ProjectsProjectID])
OUTPUT N'ProjectMinistryLinkAdded', inserted.[ProjectsProjectID], inserted.[MinistriesCode], N'owner mirror row was missing'
INTO   [dbo].[MinistryOwnershipMigrationLog] ([Action], [ProjectID], [MinistryCode], [Detail])
SELECT p.[MinistryCode], p.[ProjectID]
FROM   [Projects] p
WHERE  p.[MinistryCode] IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM [ProjectMinistries] pm
                   WHERE pm.[ProjectsProjectID] = p.[ProjectID]
                     AND pm.[MinistriesCode] = p.[MinistryCode]);

-- C. User back-fill: NULL code, not an administrator, MinistryName matches exactly one ministry.
;WITH admins AS (
    SELECT ur.[UserId]
    FROM   [AspNetUserRoles] ur
    INNER JOIN [AspNetRoles] r ON r.[Id] = ur.[RoleId]
    WHERE  r.[Name] = N'SystemAdministrator'
), matches AS (
    SELECT u.[Id] AS [UserId], m.[Code]
    FROM   [AspNetUsers] u
    INNER JOIN [Ministries] m
        ON NULLIF(LTRIM(RTRIM(u.[MinistryName])), N'') IN
           (LTRIM(RTRIM(m.[MinistryDisplayName_EN])),
            LTRIM(RTRIM(m.[MinistryDisplayName_AR])),
            LTRIM(RTRIM(m.[MinistryUserName])))
    WHERE  u.[MinistryCode] IS NULL
      AND  NOT EXISTS (SELECT 1 FROM admins a WHERE a.[UserId] = u.[Id])
), resolved AS (
    SELECT [UserId], MIN([Code]) AS [Code]
    FROM   matches
    GROUP BY [UserId]
    HAVING COUNT(DISTINCT [Code]) = 1
)
UPDATE u
SET    u.[MinistryCode] = r.[Code]
OUTPUT N'UserMinistryBackfill', inserted.[Id], inserted.[MinistryCode],
       LEFT(N'MinistryName=' + ISNULL(inserted.[MinistryName], N''), 400)
INTO   [dbo].[MinistryOwnershipMigrationLog] ([Action], [UserId], [MinistryCode], [Detail])
FROM   [AspNetUsers] u
INNER JOIN resolved r ON r.[UserId] = u.[Id]
WHERE  u.[MinistryCode] IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // See Up(): the AspNetUsers update below needs QUOTED_IDENTIFIER ON under sqlcmd too.
            migrationBuilder.Sql("SET QUOTED_IDENTIFIER ON;");

            // Replays the log. Each step only reverts a row that still holds the value Up() wrote,
            // so edits made after the migration are never undone. Removed mirror rows ARE restored.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[dbo].[MinistryOwnershipMigrationLog]', N'U') IS NOT NULL
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [ProjectMinistries] ([MinistriesCode], [ProjectsProjectID])
    SELECT DISTINCT l.[MinistryCode], l.[ProjectID]
    FROM   [dbo].[MinistryOwnershipMigrationLog] l
    WHERE  l.[Action] = N'ProjectMinistryLinkRemoved'
      AND  EXISTS (SELECT 1 FROM [Projects] p   WHERE p.[ProjectID] = l.[ProjectID])
      AND  EXISTS (SELECT 1 FROM [Ministries] m WHERE m.[Code] = l.[MinistryCode])
      AND  NOT EXISTS (SELECT 1 FROM [ProjectMinistries] pm
                       WHERE pm.[ProjectsProjectID] = l.[ProjectID]
                         AND pm.[MinistriesCode] = l.[MinistryCode]);

    -- Before the owner revert below: this matches on the owner still being the logged one.
    DELETE pm
    FROM   [ProjectMinistries] pm
    INNER JOIN [dbo].[MinistryOwnershipMigrationLog] l
            ON l.[Action] = N'ProjectMinistryLinkAdded'
           AND l.[ProjectID] = pm.[ProjectsProjectID]
           AND l.[MinistryCode] = pm.[MinistriesCode]
    INNER JOIN [Projects] p
            ON p.[ProjectID] = pm.[ProjectsProjectID]
           AND p.[MinistryCode] = l.[MinistryCode];

    UPDATE p
    SET    p.[MinistryCode] = NULL
    FROM   [Projects] p
    INNER JOIN [dbo].[MinistryOwnershipMigrationLog] l
            ON l.[Action] = N'ProjectOwnerBackfill'
           AND l.[ProjectID] = p.[ProjectID]
           AND p.[MinistryCode] = l.[MinistryCode];

    UPDATE u
    SET    u.[MinistryCode] = NULL
    FROM   [AspNetUsers] u
    INNER JOIN [dbo].[MinistryOwnershipMigrationLog] l
            ON l.[Action] = N'UserMinistryBackfill'
           AND l.[UserId] = u.[Id]
           AND u.[MinistryCode] = l.[MinistryCode];

    DROP TABLE [dbo].[MinistryOwnershipMigrationLog];
END");
        }
    }
}
