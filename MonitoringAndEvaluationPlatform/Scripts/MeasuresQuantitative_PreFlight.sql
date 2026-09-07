/*
    PRE-FLIGHT AUDIT -- MakeAllMeasuresQuantitative
    ---------------------------------------------
    READ-ONLY. Run this on EVERY database before applying migration
    20260907105050_MakeAllMeasuresQuantitative, and keep the output.

    The migration back-fills quantity-less ("Qualitative") measures so that every measure is
    Quantitative. It is Value-preserving by construction: a phase with no target is given
    TargetQuantity = 100 and each row's percentage becomes its quantity, so

        Value  ==  (Value * Target / 100) / Target * 100

    What this script tells you is WHICH of that arithmetic will be meaningful on this particular
    database and which will be merely correct. The answer differs per database -- a database with
    no CLASS B or CLASS D rows migrates cleanly and needs no review.

    The migration tags every row it back-fills in Measure.Note, so the review list also survives
    in the database itself (see MeasuresQuantitative_PostCheck.sql section 3b). Saving this
    script's output is still worth doing: section 8 is the only baseline you can diff against.

    Usage:
        sqlcmd -S <server> -d <database> -U <user> -P <pw> -i MeasuresQuantitative_PreFlight.sql -W -s "|"
*/

SET NOCOUNT ON;

PRINT '=== 0. Scale ===';
SELECT
    (SELECT COUNT(*) FROM Measures)                                              AS measures_total,
    (SELECT COUNT(*) FROM Measures WHERE Quantity IS NULL)                       AS measures_to_backfill,
    (SELECT COUNT(*) FROM Measures WHERE MeasureType <> 1)                       AS measures_to_restamp,
    (SELECT COUNT(*) FROM ProjectPhases)                                         AS phases_total;

PRINT '';
PRINT '=== 1. CLASS A -- safe back-fill (phase has no usable target) ===';
PRINT '    These get TargetQuantity = 100 and a generic "Percent" unit. Nothing is invented:';
PRINT '    the quantity IS the percentage the user originally typed. No review needed.';
SELECT COUNT(*) AS class_a_rows, COUNT(DISTINCT m.ProjectPhaseId) AS class_a_phases
FROM Measures m
JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE m.Quantity IS NULL
  AND (p.TargetQuantity IS NULL OR p.TargetQuantity <= 0);

PRINT '';
PRINT '=== 2. CLASS B -- FABRICATED QUANTITIES (phase already has a real target) ===';
PRINT '    *** THIS IS THE ONE TO LOOK AT. ***';
PRINT '    A quantity-less row on a phase that already has a real target in a real unit gets a';
PRINT '    quantity expressed in that unit (e.g. "3.7 schools"). Arithmetically correct and the';
PRINT '    percentage is preserved, but it is a number nobody collected. Review these rows, or';
PRINT '    correct them by hand after migrating.';
SELECT COUNT(*) AS class_b_rows, COUNT(DISTINCT m.ProjectPhaseId) AS class_b_phases
FROM Measures m
JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE m.Quantity IS NULL
  AND p.TargetQuantity > 0;

PRINT '';
PRINT '    CLASS B detail (up to 200 rows) -- quantity_after is what the migration will write:';
SELECT TOP 200
    m.Code,
    LEFT(m.Name, 40)                              AS Name,
    m.ProjectPhaseId,
    m.Value                                       AS value_percent,
    p.TargetQuantity                              AS phase_target,
    m.Value * p.TargetQuantity / 100.0            AS quantity_after,
    u.EN_Name                                     AS phase_unit
FROM Measures m
JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
LEFT JOIN MeasurementUnits u ON u.Code = (
    SELECT TOP 1 s.UnitCode FROM Measures s
    WHERE s.ProjectPhaseId = m.ProjectPhaseId AND s.UnitCode IS NOT NULL ORDER BY s.Code)
WHERE m.Quantity IS NULL
  AND p.TargetQuantity > 0
ORDER BY m.ProjectPhaseId, m.Code;

PRINT '';
PRINT '=== 3. CLASS C -- mixed phases (some rows have a quantity, some do not) ===';
PRINT '    Only possible on rows predating AddMeasureType. Not an error, but these phases end up';
PRINT '    with a mix of collected and derived quantities under one unit. Overlaps CLASS A/B.';
SELECT COUNT(*) AS class_c_phases FROM (
    SELECT m.ProjectPhaseId
    FROM Measures m
    GROUP BY m.ProjectPhaseId
    HAVING SUM(CASE WHEN m.Quantity IS NULL THEN 1 ELSE 0 END) > 0
       AND SUM(CASE WHEN m.Quantity IS NULL THEN 0 ELSE 1 END) > 0
) x;

PRINT '';
PRINT '=== 4. CLASS D -- rows whose Value is outside 0-100 ===';
PRINT '    The Range(0,100) rule is enforced in the app, not by a CHECK constraint, so older or';
PRINT '    hand-edited data may sit outside it. The migration does not change Value, so the';
PRINT '    database stays as-is; but the next time someone edits such a row the app recomputes';
PRINT '    and clamps it to 100, which will move that phase''s performance. Decide deliberately.';
SELECT COUNT(*) AS class_d_rows
FROM Measures WHERE Value < 0 OR Value > 100;

SELECT TOP 100 Code, LEFT(Name,40) AS Name, ProjectPhaseId, Value, Quantity
FROM Measures WHERE Value < 0 OR Value > 100 ORDER BY Code;

PRINT '';
PRINT '=== 5. Pre-existing drift (NOT caused by this migration) ===';
PRINT '    Quantitative rows whose stored Value already disagrees with Quantity / Target,';
PRINT '    normally because the phase target was edited after the measure was recorded.';
PRINT '    Listed so you do not mistake them for migration damage afterwards.';
SELECT COUNT(*) AS pre_existing_mismatch_rows
FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE m.Quantity IS NOT NULL AND p.TargetQuantity > 0
  AND ABS(m.Value - (CASE WHEN m.Quantity / p.TargetQuantity * 100 > 100
                          THEN 100 ELSE m.Quantity / p.TargetQuantity * 100 END)) > 0.000001;

PRINT '';
PRINT '=== 6. Name collision on the "Percent" unit ===';
PRINT '    MeasurementUnits.EN_Name is UNIQUE. The migration inserts "Percent" only if absent.';
PRINT '    If a row already exists here, the migration reuses it - check it means what you expect.';
SELECT Code, EN_Name, AR_Name, FR_Name FROM MeasurementUnits WHERE EN_Name = N'Percent';

PRINT '';
PRINT '=== 7. Migration state ===';
PRINT '    Confirms AddMeasureType is present and MakeAllMeasuresQuantitative is not yet applied.';
SELECT MigrationId FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%AddMeasureType%' OR MigrationId LIKE '%MakeAllMeasuresQuantitative%'
ORDER BY MigrationId;

PRINT '';
PRINT '=== 8. BASELINE -- save this output and diff it after migrating ===';
PRINT '    Every one of these numbers must be unchanged by the migration.';
SELECT CAST(SUM(Value) AS decimal(18,6)) AS grand_total_measure_value FROM Measures;
SELECT ProjectPhaseId, CAST(SUM(Value) AS decimal(18,6)) AS phase_sum
FROM Measures GROUP BY ProjectPhaseId ORDER BY ProjectPhaseId;
SELECT Id, CAST(ISNULL(PhasePerformance,0) AS decimal(18,6)) AS phase_performance
FROM ProjectPhases ORDER BY Id;
SELECT ProjectID, CAST(ISNULL(performance,0) AS decimal(18,6)) AS project_performance
FROM Projects ORDER BY ProjectID;
