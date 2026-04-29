using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMinistryStrategyManagerScoping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MinistryCode",
                table: "Frameworks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinistryCode",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Frameworks_MinistryCode",
                table: "Frameworks",
                column: "MinistryCode");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MinistryCode",
                table: "AspNetUsers",
                column: "MinistryCode");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Ministries_MinistryCode",
                table: "AspNetUsers",
                column: "MinistryCode",
                principalTable: "Ministries",
                principalColumn: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_Frameworks_Ministries_MinistryCode",
                table: "Frameworks",
                column: "MinistryCode",
                principalTable: "Ministries",
                principalColumn: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Ministries_MinistryCode",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Frameworks_Ministries_MinistryCode",
                table: "Frameworks");

            migrationBuilder.DropIndex(
                name: "IX_Frameworks_MinistryCode",
                table: "Frameworks");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_MinistryCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MinistryCode",
                table: "Frameworks");

            migrationBuilder.DropColumn(
                name: "MinistryCode",
                table: "AspNetUsers");
        }
    }
}
