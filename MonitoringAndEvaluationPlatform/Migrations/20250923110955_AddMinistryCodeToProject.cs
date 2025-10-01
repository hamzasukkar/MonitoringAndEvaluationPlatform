using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMinistryCodeToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinistryCode",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FundingPercentage",
                table: "ProjectDonors",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_MinistryCode",
                table: "Projects",
                column: "MinistryCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Ministries_MinistryCode",
                table: "Projects",
                column: "MinistryCode",
                principalTable: "Ministries",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Ministries_MinistryCode",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_MinistryCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "MinistryCode",
                table: "Projects");

            migrationBuilder.AlterColumn<decimal>(
                name: "FundingPercentage",
                table: "ProjectDonors",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
