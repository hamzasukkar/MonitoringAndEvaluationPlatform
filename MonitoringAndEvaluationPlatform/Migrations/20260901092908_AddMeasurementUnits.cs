using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <summary>
    /// Replaces the free-text Unit column on FrameworkGoals, ImpactIndicators and Measures with
    /// a foreign key into the new shared MeasurementUnits table.
    ///
    /// The operations below are deliberately hand-ordered. Scaffolding put DropColumn("Unit")
    /// first, which would have thrown away every unit anyone had typed; instead the new table
    /// and columns are created, the old strings are lifted into the table and matched back, and
    /// only then are the old columns dropped.
    /// </summary>
    public partial class AddMeasurementUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. The new lookup table ──────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "MeasurementUnits",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EN_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AR_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FR_Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasurementUnits", x => x.Code);
                });

            // ── 2. The new FK columns, alongside the old string columns ──────────────────
            migrationBuilder.AddColumn<int>(
                name: "UnitCode",
                table: "Measures",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitCode",
                table: "ImpactIndicators",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitCode",
                table: "FrameworkGoals",
                type: "int",
                nullable: true);

            // ── 3. Lift the existing free text into the lookup, then point the rows at it ─
            // Every distinct unit anyone had typed becomes a row. The English and Arabic
            // columns both get the original string: which language it was typed in is not
            // recorded anywhere, and a half-empty row would render as blank in one language.
            // An admin can correct the translations afterwards in /Units.
            //
            // Runs before the unique index is created, so a database whose collation makes two
            // spellings collide cannot fail the migration partway through — the INSERT's own
            // DISTINCT resolves them under that same collation.
            migrationBuilder.Sql(@"
INSERT INTO MeasurementUnits (EN_Name, AR_Name)
SELECT DISTINCT LTRIM(RTRIM(source.Unit)), LTRIM(RTRIM(source.Unit))
FROM (
    SELECT Unit FROM FrameworkGoals   WHERE Unit IS NOT NULL AND LTRIM(RTRIM(Unit)) <> ''
    UNION
    SELECT Unit FROM ImpactIndicators WHERE Unit IS NOT NULL AND LTRIM(RTRIM(Unit)) <> ''
    UNION
    SELECT Unit FROM Measures         WHERE Unit IS NOT NULL AND LTRIM(RTRIM(Unit)) <> ''
) AS source;

UPDATE target
SET    target.UnitCode = unit.Code
FROM   FrameworkGoals AS target
       INNER JOIN MeasurementUnits AS unit ON unit.EN_Name = LTRIM(RTRIM(target.Unit))
WHERE  target.Unit IS NOT NULL AND LTRIM(RTRIM(target.Unit)) <> '';

UPDATE target
SET    target.UnitCode = unit.Code
FROM   ImpactIndicators AS target
       INNER JOIN MeasurementUnits AS unit ON unit.EN_Name = LTRIM(RTRIM(target.Unit))
WHERE  target.Unit IS NOT NULL AND LTRIM(RTRIM(target.Unit)) <> '';

UPDATE target
SET    target.UnitCode = unit.Code
FROM   Measures AS target
       INNER JOIN MeasurementUnits AS unit ON unit.EN_Name = LTRIM(RTRIM(target.Unit))
WHERE  target.Unit IS NOT NULL AND LTRIM(RTRIM(target.Unit)) <> '';
");

            // ── 4. Only now is the old text safe to remove ───────────────────────────────
            migrationBuilder.DropColumn(
                name: "Unit",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "ImpactIndicators");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "FrameworkGoals");

            // ── 5. Indexes and constraints ───────────────────────────────────────────────
            migrationBuilder.CreateIndex(
                name: "IX_Measures_UnitCode",
                table: "Measures",
                column: "UnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_ImpactIndicators_UnitCode",
                table: "ImpactIndicators",
                column: "UnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkGoals_UnitCode",
                table: "FrameworkGoals",
                column: "UnitCode");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_EN_Name",
                table: "MeasurementUnits",
                column: "EN_Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FrameworkGoals_MeasurementUnits_UnitCode",
                table: "FrameworkGoals",
                column: "UnitCode",
                principalTable: "MeasurementUnits",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ImpactIndicators_MeasurementUnits_UnitCode",
                table: "ImpactIndicators",
                column: "UnitCode",
                principalTable: "MeasurementUnits",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Measures_MeasurementUnits_UnitCode",
                table: "Measures",
                column: "UnitCode",
                principalTable: "MeasurementUnits",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FrameworkGoals_MeasurementUnits_UnitCode",
                table: "FrameworkGoals");

            migrationBuilder.DropForeignKey(
                name: "FK_ImpactIndicators_MeasurementUnits_UnitCode",
                table: "ImpactIndicators");

            migrationBuilder.DropForeignKey(
                name: "FK_Measures_MeasurementUnits_UnitCode",
                table: "Measures");

            // Put the text columns back and refill them before anything is dropped, so rolling
            // back is as lossless as rolling forward. Units nobody referenced are the one thing
            // that cannot survive — there is nowhere in the old schema to keep them.
            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "Measures",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "ImpactIndicators",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "FrameworkGoals",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE target
SET    target.Unit = unit.EN_Name
FROM   FrameworkGoals AS target
       INNER JOIN MeasurementUnits AS unit ON unit.Code = target.UnitCode;

UPDATE target
SET    target.Unit = unit.EN_Name
FROM   ImpactIndicators AS target
       INNER JOIN MeasurementUnits AS unit ON unit.Code = target.UnitCode;

UPDATE target
SET    target.Unit = unit.EN_Name
FROM   Measures AS target
       INNER JOIN MeasurementUnits AS unit ON unit.Code = target.UnitCode;
");

            migrationBuilder.DropIndex(
                name: "IX_Measures_UnitCode",
                table: "Measures");

            migrationBuilder.DropIndex(
                name: "IX_ImpactIndicators_UnitCode",
                table: "ImpactIndicators");

            migrationBuilder.DropIndex(
                name: "IX_FrameworkGoals_UnitCode",
                table: "FrameworkGoals");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "Measures");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "ImpactIndicators");

            migrationBuilder.DropColumn(
                name: "UnitCode",
                table: "FrameworkGoals");

            migrationBuilder.DropTable(
                name: "MeasurementUnits");
        }
    }
}
