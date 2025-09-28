using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class organizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonorCountries",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorCountries", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "UNorganizations",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UNorganizations", x => x.Code);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorCountries");

            migrationBuilder.DropTable(
                name: "UNorganizations");
        }
    }
}
