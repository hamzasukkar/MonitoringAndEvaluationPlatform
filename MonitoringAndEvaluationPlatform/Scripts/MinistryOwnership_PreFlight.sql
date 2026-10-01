/*
    PRE-FLIGHT AUDIT -- BackfillMinistryOwnership
    ---------------------------------------------
    READ-ONLY. Run this on EVERY database before applying migration
    20260929110858_BackfillMinistryOwnership, and keep the output.

    Projects.MinistryCode is the single owner of a project; the ProjectMinistries table is a
    back-compat mirror of it. Ministry isolation reads the owner only. The migration:
      A. fills Projects.MinistryCode where it is NULL and the project has exactly ONE mirror row;
      B. re-syncs every OWNED project's mirror rows to exactly { MinistryCode };
      C. fills AspNetUsers.MinistryCode where it is NULL and MinistryName matches exactly one
         ministry (English name, Arabic name or MinistryUserName); administrators are skipped.
    It never overwrites a non-NULL MinistryCode, never picks an owner for a project with 2+ mirror
    rows, and never touches a project with none.

    What to review before migrating:
      - Section 3 (ambiguous) and 4 (orphans): these projects stay ownerless, i.e. visible to
        administrators only, until someone assigns their ministry in Projects/Edit.
      - Section 5a: the mirror rows the migration DELETES. Save this output -- together with the
        migration's own log table it is the record of what was there.
      - Section 6 (class E): projects owned by one ministry but linked under another ministry's
        strategy. The migration does not change them; fix them by hand for complete isolation.
      - Section 7: users whose ministry cannot be resolved stay without one and see no data.

    Usage:
        sqlcmd -S <server> -d <database> -U <user> -P <pw> -f 65001 -i MinistryOwnership_PreFlight.sql -W -s "|" -o preflight_<db>.txt
*/

SET NOCOUNT ON;
-- sqlcmd runs with QUOTED_IDENTIFIER OFF, which the FOR XML ... .value() role lists need ON.
SET QUOTED_IDENTIFIER ON;

PRINT '=== 0. Migration state (expect: no BackfillMinistryOwnership row, log table absent) ===';
SELECT MigrationId FROM __EFMigrationsHistory WHERE MigrationId LIKE N'%BackfillMinistryOwnership%';
SELECT TOP 3 MigrationId AS latest_applied FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
SELECT CASE WHEN OBJECT_ID(N'dbo.MinistryOwnershipMigrationLog', N'U') IS NULL
            THEN 'absent (expected)' ELSE 'PRESENT - investigate before migrating' END AS log_table;

PRINT '';
PRINT '=== 1. Scale ===';
SELECT
    (SELECT COUNT(*) FROM Projects)                                                     AS projects_total,
    (SELECT COUNT(*) FROM Projects WHERE MinistryCode IS NULL)                          AS projects_owner_null,
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NULL
       AND (SELECT COUNT(*) FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID) = 1)  AS class_a_will_backfill,
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NULL
       AND (SELECT COUNT(*) FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID) >= 2) AS class_b_ambiguous,
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NULL
       AND NOT EXISTS (SELECT 1 FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID))  AS class_c_orphans,
    (SELECT COUNT(*) FROM ProjectMinistries pm JOIN Projects p ON p.ProjectID = pm.ProjectsProjectID
       WHERE p.MinistryCode IS NOT NULL AND pm.MinistriesCode <> p.MinistryCode)                      AS class_d_rows_to_remove,
    (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM ProjectMinistries pm
                       WHERE pm.ProjectsProjectID = p.ProjectID AND pm.MinistriesCode = p.MinistryCode)) AS class_d_rows_to_add,
    (SELECT COUNT(*) FROM AspNetUsers)                                                  AS users_total,
    (SELECT COUNT(*) FROM AspNetUsers WHERE MinistryCode IS NULL)                       AS users_ministry_null;

PRINT '';
PRINT '=== 2. CLASS A -- owner NULL, exactly one mirror row: WILL BE BACK-FILLED to that ministry ===';
PRINT '    conflicts_with_strategy = YES: a linked indicator sits under ANOTHER ministry''s strategy,';
PRINT '    so after the back-fill the project will also appear in section 6. Review before migrating.';
SELECT p.ProjectID, LEFT(p.ProjectName, 60) AS ProjectName,
       pm.MinistriesCode AS will_become_owner, m.MinistryUserName, LEFT(m.MinistryDisplayName_EN, 50) AS MinistryEN,
       CASE WHEN EXISTS (
            SELECT 1 FROM Indicators i
            JOIN SubOutputs so ON so.Code = i.SubOutputCode
            JOIN Outputs op    ON op.Code = so.OutputCode
            JOIN Outcomes oc   ON oc.Code = op.OutcomeCode
            JOIN Frameworks f  ON f.Code  = oc.FrameworkCode
            WHERE i.ProjectID = p.ProjectID AND f.MinistryCode IS NOT NULL AND f.MinistryCode <> pm.MinistriesCode)
            THEN 'YES' ELSE '' END AS conflicts_with_strategy
FROM Projects p
JOIN ProjectMinistries pm ON pm.ProjectsProjectID = p.ProjectID
JOIN Ministries m         ON m.Code = pm.MinistriesCode
WHERE p.MinistryCode IS NULL
  AND (SELECT COUNT(*) FROM ProjectMinistries x WHERE x.ProjectsProjectID = p.ProjectID) = 1
ORDER BY p.ProjectID;

PRINT '';
PRINT '=== 3. CLASS B -- owner NULL, 2+ mirror rows: AMBIGUOUS, LEFT UNASSIGNED (admin-only until assigned) ===';
PRINT '    One row per candidate ministry. strategy_owner_hint = the single ministry that owns every';
PRINT '    strategy this project''s indicators sit under (NULL if none or several). Assign in Projects/Edit.';
SELECT p.ProjectID, LEFT(p.ProjectName, 60) AS ProjectName,
       pm.MinistriesCode AS candidate_ministry, m.MinistryUserName, LEFT(m.MinistryDisplayName_EN, 50) AS MinistryEN,
       (SELECT CASE WHEN COUNT(DISTINCT f.MinistryCode) = 1 THEN MIN(f.MinistryCode) END
          FROM Indicators i
          JOIN SubOutputs so ON so.Code = i.SubOutputCode
          JOIN Outputs op    ON op.Code = so.OutputCode
          JOIN Outcomes oc   ON oc.Code = op.OutcomeCode
          JOIN Frameworks f  ON f.Code  = oc.FrameworkCode
         WHERE i.ProjectID = p.ProjectID AND f.MinistryCode IS NOT NULL) AS strategy_owner_hint
FROM Projects p
JOIN ProjectMinistries pm ON pm.ProjectsProjectID = p.ProjectID
JOIN Ministries m         ON m.Code = pm.MinistriesCode
WHERE p.MinistryCode IS NULL
  AND (SELECT COUNT(*) FROM ProjectMinistries x WHERE x.ProjectsProjectID = p.ProjectID) >= 2
ORDER BY p.ProjectID, pm.MinistriesCode;

PRINT '';
PRINT '=== 4. CLASS C -- owner NULL, no mirror row: ORPHANS, UNCHANGED (admin-only until assigned) ===';
SELECT p.ProjectID, LEFT(p.ProjectName, 60) AS ProjectName,
       (SELECT CASE WHEN COUNT(DISTINCT f.MinistryCode) = 1 THEN MIN(f.MinistryCode) END
          FROM Indicators i
          JOIN SubOutputs so ON so.Code = i.SubOutputCode
          JOIN Outputs op    ON op.Code = so.OutputCode
          JOIN Outcomes oc   ON oc.Code = op.OutcomeCode
          JOIN Frameworks f  ON f.Code  = oc.FrameworkCode
         WHERE i.ProjectID = p.ProjectID AND f.MinistryCode IS NOT NULL) AS strategy_owner_hint
FROM Projects p
WHERE p.MinistryCode IS NULL
  AND NOT EXISTS (SELECT 1 FROM ProjectMinistries pm WHERE pm.ProjectsProjectID = p.ProjectID)
ORDER BY p.ProjectID;

PRINT '';
PRINT '=== 5. CLASS D -- owned projects whose mirror differs from the owner: WILL BE RE-SYNCED ===';
PRINT '    5a. Mirror rows the migration DELETES. SAVE THIS OUTPUT.';
SELECT pm.ProjectsProjectID AS ProjectID, LEFT(p.ProjectName, 60) AS ProjectName,
       p.MinistryCode AS owner_code, mo.MinistryUserName AS owner,
       pm.MinistriesCode AS removed_link_code, ms.MinistryUserName AS removed_link
FROM ProjectMinistries pm
JOIN Projects p    ON p.ProjectID = pm.ProjectsProjectID
JOIN Ministries mo ON mo.Code = p.MinistryCode
JOIN Ministries ms ON ms.Code = pm.MinistriesCode
WHERE p.MinistryCode IS NOT NULL AND pm.MinistriesCode <> p.MinistryCode
ORDER BY pm.ProjectsProjectID, pm.MinistriesCode;

PRINT '    5b. Owner mirror rows the migration ADDS.';
SELECT p.ProjectID, LEFT(p.ProjectName, 60) AS ProjectName, p.MinistryCode AS owner_code, m.MinistryUserName AS owner
FROM Projects p
JOIN Ministries m ON m.Code = p.MinistryCode
WHERE NOT EXISTS (SELECT 1 FROM ProjectMinistries pm
                  WHERE pm.ProjectsProjectID = p.ProjectID AND pm.MinistriesCode = p.MinistryCode)
ORDER BY p.ProjectID;

PRINT '';
PRINT '=== 6. CLASS E -- project owner <> owner of the strategy its indicator sits under (REPORT ONLY) ===';
PRINT '    Not changed by the migration. The application hides the other ministry''s side of each';
PRINT '    link, but the counts on strategy pages can still include these. Resolve them by hand.';
SELECT DISTINCT p.ProjectID, LEFT(p.ProjectName, 50) AS ProjectName,
       p.MinistryCode AS project_owner, pmn.MinistryUserName AS project_owner_name,
       i.IndicatorCode, f.Code AS FrameworkCode, LEFT(f.Name, 50) AS FrameworkName,
       f.MinistryCode AS strategy_owner, fmn.MinistryUserName AS strategy_owner_name
FROM Projects p
JOIN Indicators i   ON i.ProjectID = p.ProjectID
JOIN SubOutputs so  ON so.Code = i.SubOutputCode
JOIN Outputs op     ON op.Code = so.OutputCode
JOIN Outcomes oc    ON oc.Code = op.OutcomeCode
JOIN Frameworks f   ON f.Code  = oc.FrameworkCode
JOIN Ministries pmn ON pmn.Code = p.MinistryCode
JOIN Ministries fmn ON fmn.Code = f.MinistryCode
WHERE p.MinistryCode <> f.MinistryCode
ORDER BY p.ProjectID, i.IndicatorCode;

PRINT '    6b. Owned projects linked under a strategy with NO owner (that strategy is admin-only).';
SELECT DISTINCT p.ProjectID, LEFT(p.ProjectName, 50) AS ProjectName, p.MinistryCode AS project_owner,
       f.Code AS FrameworkCode, LEFT(f.Name, 50) AS FrameworkName
FROM Projects p
JOIN Indicators i  ON i.ProjectID = p.ProjectID
JOIN SubOutputs so ON so.Code = i.SubOutputCode
JOIN Outputs op    ON op.Code = so.OutputCode
JOIN Outcomes oc   ON oc.Code = op.OutcomeCode
JOIN Frameworks f  ON f.Code  = oc.FrameworkCode
WHERE p.MinistryCode IS NOT NULL AND f.MinistryCode IS NULL
ORDER BY p.ProjectID;

PRINT '';
PRINT '=== 7. CLASS F -- non-admin users with MinistryCode NULL ===';
;WITH admins AS (
    SELECT ur.UserId FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId
    WHERE r.Name = N'SystemAdministrator'
), candidates AS (
    SELECT u.Id, u.UserName, u.MinistryName FROM AspNetUsers u
    WHERE u.MinistryCode IS NULL AND NOT EXISTS (SELECT 1 FROM admins a WHERE a.UserId = u.Id)
), matches AS (
    SELECT c.Id, m.Code FROM candidates c
    JOIN Ministries m ON NULLIF(LTRIM(RTRIM(c.MinistryName)), N'') IN
        (LTRIM(RTRIM(m.MinistryDisplayName_EN)), LTRIM(RTRIM(m.MinistryDisplayName_AR)), LTRIM(RTRIM(m.MinistryUserName)))
)
SELECT CASE COUNT(DISTINCT mt.Code) WHEN 1 THEN '1 RESOLVABLE (back-filled)'
                                    WHEN 0 THEN '3 UNMATCHED (stays NULL: sees nothing)'
                                    ELSE        '2 AMBIGUOUS (stays NULL: sees nothing)' END AS outcome,
       c.UserName, c.MinistryName,
       CASE WHEN COUNT(DISTINCT mt.Code) = 1 THEN MIN(mt.Code) END AS will_become_ministry_code,
       COUNT(DISTINCT mt.Code) AS matching_ministries,
       STUFF((SELECT N',' + r.Name FROM AspNetUserRoles ur JOIN AspNetRoles r ON r.Id = ur.RoleId
              WHERE ur.UserId = c.Id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, N'') AS roles
FROM candidates c
LEFT JOIN matches mt ON mt.Id = c.Id
GROUP BY c.Id, c.UserName, c.MinistryName
ORDER BY outcome, c.UserName;

PRINT '    7b. Informational: MinistryCode set but MinistryName names something else (not changed).';
SELECT u.UserName, u.MinistryCode, m.MinistryUserName, u.MinistryName
FROM AspNetUsers u JOIN Ministries m ON m.Code = u.MinistryCode
WHERE ISNULL(LTRIM(RTRIM(u.MinistryName)), N'') NOT IN
      (LTRIM(RTRIM(m.MinistryDisplayName_EN)), LTRIM(RTRIM(m.MinistryDisplayName_AR)), LTRIM(RTRIM(m.MinistryUserName)));

PRINT '';
PRINT '=== 8. Behaviour preview -- strategies a ministry reaches TODAY only through a project link ===';
PRINT '    After the release, ministry users see only strategies whose Frameworks.MinistryCode is theirs.';
SELECT DISTINCT pm.MinistriesCode AS ministry, mm.MinistryUserName,
       f.Code AS FrameworkCode, LEFT(f.Name, 60) AS FrameworkName, f.MinistryCode AS framework_owner
FROM Frameworks f
JOIN Outcomes oc          ON oc.FrameworkCode = f.Code
JOIN Outputs op           ON op.OutcomeCode = oc.Code
JOIN SubOutputs so        ON so.OutputCode = op.Code
JOIN Indicators i         ON i.SubOutputCode = so.Code
JOIN ProjectMinistries pm ON pm.ProjectsProjectID = i.ProjectID
JOIN Ministries mm        ON mm.Code = pm.MinistriesCode
WHERE f.MinistryCode IS NULL OR f.MinistryCode <> pm.MinistriesCode
ORDER BY ministry, FrameworkCode;
SELECT COUNT(*) AS ownerless_strategies FROM Frameworks WHERE MinistryCode IS NULL;

PRINT '';
PRINT '=== 9. BASELINE -- save; compare after the migration and the recalculation ===';
SELECT m.Code, m.MinistryUserName,
       CAST(m.IndicatorsPerformance   AS decimal(18,6)) AS stored_indicators,
       CAST(m.DisbursementPerformance AS decimal(18,6)) AS stored_disbursement,
       (SELECT COUNT(*) FROM Projects p WHERE p.MinistryCode = m.Code)               AS owned_projects,
       (SELECT COUNT(*) FROM ProjectMinistries pm WHERE pm.MinistriesCode = m.Code)  AS mirror_rows
FROM Ministries m ORDER BY m.Code;
SELECT COUNT(*) AS project_ministry_rows FROM ProjectMinistries;
