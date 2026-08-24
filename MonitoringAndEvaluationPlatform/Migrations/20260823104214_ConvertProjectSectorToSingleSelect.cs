using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class ConvertProjectSectorToSingleSelect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable at first so existing projects can be backfilled from ProjectSectors
            // before the column becomes NOT NULL.
            migrationBuilder.AddColumn<int>(
                name: "SectorCode",
                table: "Projects",
                type: "int",
                nullable: true);

            // A project may previously have had several sectors; a single sector is now
            // required, so the lowest sector Code wins for any project that had more than one.
            // This is a deliberate, lossy collapse — projects that had multiple sectors lose
            // all but the lowest-coded one.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.SectorCode = agg.MinCode
                FROM Projects p
                INNER JOIN (
                    SELECT ProjectsProjectID, MIN(SectorsCode) AS MinCode
                    FROM ProjectSectors
                    GROUP BY ProjectsProjectID
                ) agg ON agg.ProjectsProjectID = p.ProjectID;
            ");

            // Defensive fallback for any project with no rows in ProjectSectors (shouldn't exist,
            // since a sector was already required, but avoids a failed NOT NULL conversion below).
            migrationBuilder.Sql(@"
                UPDATE Projects
                SET SectorCode = (SELECT MIN(Code) FROM Sectors)
                WHERE SectorCode IS NULL;
            ");

            migrationBuilder.DropTable(
                name: "ProjectSectors");

            migrationBuilder.AlterColumn<int>(
                name: "SectorCode",
                table: "Projects",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_SectorCode",
                table: "Projects",
                column: "SectorCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Sectors_SectorCode",
                table: "Projects",
                column: "SectorCode",
                principalTable: "Sectors",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Sectors_SectorCode",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_SectorCode",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "ProjectSectors",
                columns: table => new
                {
                    ProjectsProjectID = table.Column<int>(type: "int", nullable: false),
                    SectorsCode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSectors", x => new { x.ProjectsProjectID, x.SectorsCode });
                    table.ForeignKey(
                        name: "FK_ProjectSectors_Projects_ProjectsProjectID",
                        column: x => x.ProjectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectSectors_Sectors_SectorsCode",
                        column: x => x.SectorsCode,
                        principalTable: "Sectors",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectSectors_SectorsCode",
                table: "ProjectSectors",
                column: "SectorsCode");

            // Lossy: each project only remembers the single sector it had after Up() ran, so a
            // project that previously had multiple sectors does not get them all back here.
            migrationBuilder.Sql(@"
                INSERT INTO ProjectSectors (ProjectsProjectID, SectorsCode)
                SELECT ProjectID, SectorCode FROM Projects WHERE SectorCode IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "SectorCode",
                table: "Projects");
        }
    }
}
