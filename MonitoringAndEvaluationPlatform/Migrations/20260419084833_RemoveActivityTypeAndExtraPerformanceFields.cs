using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveActivityTypeAndExtraPerformanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Targets");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "SubOutputs");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "SubOutputs");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Sectors");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "sDGIndicators");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "sDGIndicators");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Financial",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Physical",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Outputs");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Outcomes");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Outcomes");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Ministries");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Ministries");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Indicators");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Goals");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Frameworks");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Frameworks");

            migrationBuilder.DropColumn(
                name: "FieldMonitoring",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "ImpactAssessment",
                table: "Donors");

            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Targets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Targets",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "SubOutputs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "SubOutputs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Sectors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "sDGIndicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "sDGIndicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Projects",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Financial",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Projects",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Physical",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Outputs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Outputs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Outcomes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Outcomes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Ministries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Ministries",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Indicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Indicators",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Goals",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Goals",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Frameworks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Frameworks",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "FieldMonitoring",
                table: "Donors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ImpactAssessment",
                table: "Donors",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ActivityType",
                table: "Activities",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
