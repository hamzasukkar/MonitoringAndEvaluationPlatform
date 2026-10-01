/*
    POST-MIGRATION CHECK -- BackfillMinistryOwnership
    -------------------------------------------------
    READ-ONLY. Run immediately AFTER applying migration 20260929110858_BackfillMinistryOwnership.

    Sections 1-3 must all report PASS.
    Section 4 counts must equal the pre-flight counts:
        ProjectOwnerBackfill       = class_a_will_backfill      (pre-flight sec. 1)
        ProjectMinistryLinkRemoved = class_d_rows_to_remove     (pre-flight sec. 1)
        ProjectMinistryLinkAdded   = class_d_rows_to_add        (pre-flight sec. 1)
        UserMinistryBackfill       = RESOLVABLE users           (pre-flight sec. 7)
    Section 5 must be EMPTY for complete isolation (it is not changed by the migration).

    Then, in the application: Data Management -> "Recalculate Ministry Performance", once.

    Usage:
        sqlcmd -S <server> -d <database> -U <user> -P <pw> -f 65001 -i MinistryOwnership_PostCheck.sql -W -s "|"
*/

SET NOCOUNT ON;
-- sqlcmd runs with QUOTED_IDENTIFIER OFF, which the FOR XML ... .value() role lists need ON.
SET QUOTED_IDENTIFIER ON;

PRINT '=== 0. Migration applied ===';
SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE N'%BackfillMinistryOwnership%';

PRINT '';
PRINT '=== 1. Every owned project has exactly one mirror row, equal to its owner ===';
SELECT CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result, COUNT(*) AS offending_projects
FROM Projects p
WHERE p.MinistryCode IS NOT NULL
  AND (   NOT EXISTS (SELECT 1 FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID AND pm.MinistriesCode = p.MinistryCode)
       OR EXISTS     (SELECT 1 FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID AND pm.MinistriesCode <> p.MinistryCode));

PRINT '';
PRINT '=== 2. No ownerless project with exactly one mirror row remains ===';
SELECT CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result, COUNT(*) AS offending_projects
FROM Projects p
WHERE p.MinistryCode IS NULL
  AND (SELECT COUNT(*) FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID) = 1;

PRINT '';
PRINT '=== 3. No non-admin user left NULL whose MinistryName resolves to exactly one ministry ===';
;WITH admins AS (
    SELECT ur.UserId FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId WHERE r.Name = N'SystemAdministrator'
), candidates AS (
    SELECT u.Id FROM AspNetUsers u
    WHERE u.MinistryCode IS NULL AND NOT EXISTS (SELECT 1 FROM admins a WHERE a.UserId = u.Id)
), matches AS (
    SELECT c.Id, m.Code FROM candidates c
    JOIN AspNetUsers u ON u.Id = c.Id
    JOIN Ministries m ON NULLIF(LTRIM(RTRIM(u.MinistryName)), N'') IN
        (LTRIM(RTRIM(m.MinistryDisplayName_EN)), LTRIM(RTRIM(m.MinistryDisplayName_AR)), LTRIM(RTRIM(m.MinistryUserName)))
)
SELECT CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result, COUNT(*) AS offending_users
FROM (SELECT Id FROM matches GROUP BY Id HAVING COUNT(DISTINCT Code) = 1) x;

PRINT '';
PRINT '=== 4. Reconcile with pre-flight ===';
SELECT Action, COUNT(*) AS rows_logged FROM dbo.MinistryOwnershipMigrationLog GROUP BY Action ORDER BY Action;
SELECT
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NULL
       AND (SELECT COUNT(*) FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID) >= 2) AS ambiguous_left,
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NULL
       AND NOT EXISTS (SELECT 1 FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID))  AS orphans_left;

PRINT '    Non-admin users still without a ministry (they see no data until one is assigned):';
SELECT u.UserName, u.MinistryName,
       STUFF((SELECT N',' + r.Name FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId
              WHERE ur.UserId = u.Id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') AS roles
FROM AspNetUsers u
WHERE u.MinistryCode IS NULL
  AND NOT EXISTS (SELECT 1 FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId
                  WHERE ur.UserId = u.Id AND r.Name = N'SystemAdministrator')
ORDER BY u.UserName;

PRINT '';
PRINT '=== 5. WARN -- class E: project owner <> strategy owner (must be EMPTY for complete isolation) ===';
SELECT DISTINCT p.ProjectID, LEFT(p.ProjectName, 50) AS ProjectName, p.MinistryCode AS project_owner,
       i.IndicatorCode, f.Code AS FrameworkCode, LEFT(f.Name, 50) AS FrameworkName, f.MinistryCode AS strategy_owner
FROM Projects p
JOIN Indicators i  ON i.ProjectID = p.ProjectID
JOIN SubOutputs so ON so.Code = i.SubOutputCode
JOIN Outputs op    ON op.Code = so.OutputCode
JOIN Outcomes oc   ON oc.Code = op.OutcomeCode
JOIN Frameworks f  ON f.Code  = oc.FrameworkCode
WHERE p.MinistryCode IS NOT NULL AND f.MinistryCode IS NOT NULL AND p.MinistryCode <> f.MinistryCode
ORDER BY p.ProjectID;
