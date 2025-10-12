using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Ministry_EN : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MinistryDisplayName",
                table: "Ministries",
                newName: "MinistryDisplayName_EN");

            migrationBuilder.AddColumn<string>(
                name: "MinistryDisplayName_AR",
                table: "Ministries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinistryDisplayName_AR",
                table: "Ministries");

            migrationBuilder.RenameColumn(
                name: "MinistryDisplayName_EN",
                table: "Ministries",
                newName: "MinistryDisplayName");
        }
    }
}
