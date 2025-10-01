using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class donorCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DonorCountries");

            migrationBuilder.DropTable(
                name: "UNorganizations");

            migrationBuilder.AddColumn<int>(
                name: "donorCategory",
                table: "Donors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "donorCategory",
                table: "Donors");

            migrationBuilder.CreateTable(
                name: "DonorCountries",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    DisbursementPerformance = table.Column<double>(type: "float", nullable: false),
                    FieldMonitoring = table.Column<double>(type: "float", nullable: false),
                    ImpactAssessment = table.Column<double>(type: "float", nullable: false),
                    IndicatorsPerformance = table.Column<double>(type: "float", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UNorganizations", x => x.Code);
                });
        }
    }
}
