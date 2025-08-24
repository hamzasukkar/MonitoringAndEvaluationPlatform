using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Edit_Sector : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Sectors",
                newName: "EN_Name");

            migrationBuilder.AddColumn<string>(
                name: "AR_Name",
                table: "Sectors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AR_Name",
                table: "Sectors");

            migrationBuilder.RenameColumn(
                name: "EN_Name",
                table: "Sectors",
                newName: "Name");
        }
    }
}
