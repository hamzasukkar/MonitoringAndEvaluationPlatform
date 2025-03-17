using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceToMinistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ImpactAssessment",
                table: "Ministry",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "FieldMonitoring",
                table: "Ministry",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<double>(
                name: "DisbursementPerformance",
                table: "Ministry",
                type: "float",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<double>(
                name: "IndicatorsPerformance",
                table: "Ministry",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IndicatorsPerformance",
                table: "Ministry");

            migrationBuilder.AlterColumn<int>(
                name: "ImpactAssessment",
                table: "Ministry",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "FieldMonitoring",
                table: "Ministry",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<int>(
                name: "DisbursementPerformance",
                table: "Ministry",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");
        }
    }
}
