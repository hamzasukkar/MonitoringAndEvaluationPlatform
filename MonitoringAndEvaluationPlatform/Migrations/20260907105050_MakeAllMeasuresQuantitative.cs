using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <summary>
    /// Retires Qualitative measures. Every measure now carries a Quantity and a Unit, and its
    /// Value is derived from Quantity ÷ ProjectPhase.TargetQuantity × 100.
    ///
    /// The back-fill is deliberately Value-preserving: a legacy row keeps the exact percentage it
    /// had, because a phase with no target is given a target of 100 and the row's own percentage
    /// becomes its quantity — (Value × T ÷ 100) ÷ T × 100 = Value. No phase, project, indicator or
    /// framework performance number moves as a result of this migration.
    ///
    /// Caveat: on a phase that ALREADY had a real target in a real unit, a quantity-less row gets a
    /// quantity expressed in that unit (e.g. "3.7 schools"). It is arithmetically correct and
    /// preserves the percentage, but it is a number nobody actually collected. How many such rows
    /// a database has varies -- run Scripts/MeasuresQuantitative_PreFlight.sql first to find out.
    ///
    /// Every back-filled row is tagged in its Note, so the review list survives on each database
    /// rather than depending on the pre-flight output having been saved:
    ///
    ///     SELECT m.Code, m.Name, m.Quantity, p.TargetQuantity
    ///     FROM Measures m JOIN ProjectPhases p ON p.Id = m.ProjectPhaseId
    ///     WHERE m.Note LIKE '%[[]auto-derived quantity 2026-09-07]%'
    ///       AND p.TargetQuantity &lt;&gt; 100;   -- drop this line for ALL back-filled rows
    /// </summary>
    public partial class MakeAllMeasuresQuantitative : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1. A generic Percent unit for rows whose phase never had a unit of its own.
IF NOT EXISTS (SELECT 1 FROM [MeasurementUnits] WHERE [EN_Name] = N'Percent')
    INSERT INTO [MeasurementUnits] ([EN_Name], [AR_Name], [FR_Name])
    VALUES (N'Percent', N'نسبة مئوية', N'Pourcentage');

DECLARE @PercentUnit int = (SELECT TOP 1 [Code] FROM [MeasurementUnits] WHERE [EN_Name] = N'Percent' ORDER BY [Code]);

-- 2. A phase holding quantity-less measures needs a target before those can become percentages.
--    100 makes the row's existing percentage double as its quantity.
UPDATE p
SET p.[TargetQuantity] = 100
FROM [ProjectPhases] p
WHERE (p.[TargetQuantity] IS NULL OR p.[TargetQuantity] <= 0)
  AND EXISTS (SELECT 1 FROM [Measures] m WHERE m.[ProjectPhaseId] = p.[Id] AND m.[Quantity] IS NULL);

-- 3. Back-fill quantity and unit. Value is left untouched and stays correct by construction.
--    The Note marker matters: after this runs, a derived quantity is otherwise indistinguishable
--    from one a user actually collected, and the review list would be lost on every database that
--    did not save its pre-flight output. The marker is skipped only when the note has no room.
UPDATE m
SET m.[Quantity] = m.[Value] * p.[TargetQuantity] / 100.0,
    m.[UnitCode] = COALESCE(
        m.[UnitCode],
        (SELECT TOP 1 s.[UnitCode] FROM [Measures] s
         WHERE s.[ProjectPhaseId] = m.[ProjectPhaseId] AND s.[UnitCode] IS NOT NULL
         ORDER BY s.[Code]),
        @PercentUnit),
    m.[Note] = CASE
        WHEN LEN(ISNULL(m.[Note], N'')) + 35 <= 1000
            THEN ISNULL(m.[Note] + N' ', N'') + N'[auto-derived quantity 2026-09-07]'
        ELSE m.[Note]
    END
FROM [Measures] m
INNER JOIN [ProjectPhases] p ON p.[Id] = m.[ProjectPhaseId]
WHERE m.[Quantity] IS NULL
  AND p.[TargetQuantity] IS NOT NULL
  AND p.[TargetQuantity] > 0;

-- 4. Rows created before the AddMeasureType migration are stamped Qualitative regardless of
--    whether they carry a quantity. Everything is Quantitative now.
UPDATE [Measures] SET [MeasureType] = 1 WHERE [MeasureType] <> 1;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: which rows were Qualitative, and which phases had no TargetQuantity,
            // is not recorded anywhere once Up has run. Restore from a backup instead.
        }
    }
}
