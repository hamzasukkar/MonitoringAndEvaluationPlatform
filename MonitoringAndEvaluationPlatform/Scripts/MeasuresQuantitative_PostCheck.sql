/*
    POST-MIGRATION CHECK -- MakeAllMeasuresQuantitative
    --------------------------------------------------
    READ-ONLY. Run on each database immediately AFTER applying
    20260907105050_MakeAllMeasuresQuantitative.

    Sections 1-3 must all report PASS. Section 4 reproduces the pre-flight baseline: diff it
    against the section 8 output you saved from MeasuresQuantitative_PreFlight.sql. It must be
    byte-identical -- the migration is designed not to move a single performance number.

    Usage:
        sqlcmd -S <server> -d <database> -U <user> -P <pw> -i MeasuresQuantitative_PostCheck.sql -W -s "|"
*/

SET NOCOUNT ON;

PRINT '=== 1. Every measure is quantitative and has a quantity ===';
SELECT CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result, COUNT(*) AS offending_rows
FROM Measures WHERE MeasureType <> 1 OR Quantity IS NULL;

PRINT '';
PRINT '=== 2. Every phase holding measures has a usable target ===';
PRINT '    Without one the app cannot accept or edit a measure on that phase.';
SELECT CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result, COUNT(*) AS offending_phases
FROM (
    SELECT DISTINCT m.ProjectPhaseId
    FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
    WHERE p.TargetQuantity IS NULL OR p.TargetQuantity <= 0
) x;

PRINT '';
PRINT '=== 3. Value still equals Quantity / Target, except for known pre-existing drift ===';
PRINT '    Compare offending_rows against pre_existing_mismatch_rows from pre-flight section 5.';
PRINT '    Equal  -> PASS, the migration introduced no new drift.';
PRINT '    Higher -> investigate: the extra rows were moved by the migration.';
SELECT COUNT(*) AS offending_rows
FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE p.TargetQuantity > 0
  AND ABS(m.Value - (CASE WHEN m.Quantity / p.TargetQuantity * 100 > 100
                          THEN 100 ELSE m.Quantity / p.TargetQuantity * 100 END)) > 0.000001;

PRINT '';
PRINT '    Detail of the above (up to 100):';
SELECT TOP 100 m.Code, LEFT(m.Name,40) AS Name, m.ProjectPhaseId, m.Value, m.Quantity, p.TargetQuantity
FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE p.TargetQuantity > 0
  AND ABS(m.Value - (CASE WHEN m.Quantity / p.TargetQuantity * 100 > 100
                          THEN 100 ELSE m.Quantity / p.TargetQuantity * 100 END)) > 0.000001
ORDER BY m.Code;

PRINT '';
PRINT '=== 3b. CLASS B review list (from the Note marker the migration writes) ===';
PRINT '    A derived quantity is otherwise indistinguishable from a collected one, so the migration';
PRINT '    tags every row it back-fills. class_b should match class_b_rows from pre-flight sec. 2.';
SELECT
    SUM(CASE WHEN p.TargetQuantity <> 100 THEN 1 ELSE 0 END) AS class_b_needs_review,
    COUNT(*)                                                 AS all_backfilled_rows
FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
WHERE m.Note LIKE '%[[]auto-derived quantity 2026-09-07]%';

PRINT '';
PRINT '    CLASS B detail -- these carry a derived quantity in a real physical unit. Review them:';
SELECT TOP 200 m.Code, LEFT(m.Name,40) AS Name, m.ProjectPhaseId,
       m.Value AS value_percent, m.Quantity AS quantity_derived,
       p.TargetQuantity AS phase_target, u.EN_Name AS unit
FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
LEFT JOIN MeasurementUnits u ON u.Code = m.UnitCode
WHERE m.Note LIKE '%[[]auto-derived quantity 2026-09-07]%'
  AND p.TargetQuantity <> 100
ORDER BY m.ProjectPhaseId, m.Code;

PRINT '';
PRINT '=== 4. BASELINE -- diff against pre-flight section 8. Must be identical. ===';
SELECT CAST(SUM(Value) AS decimal(18,6)) AS grand_total_measure_value FROM Measures;
SELECT ProjectPhaseId, CAST(SUM(Value) AS decimal(18,6)) AS phase_sum
FROM Measures GROUP BY ProjectPhaseId ORDER BY ProjectPhaseId;
SELECT Id, CAST(ISNULL(PhasePerformance,0) AS decimal(18,6)) AS phase_performance
FROM ProjectPhases ORDER BY Id;
SELECT ProjectID, CAST(ISNULL(performance,0) AS decimal(18,6)) AS project_performance
FROM Projects ORDER BY ProjectID;
