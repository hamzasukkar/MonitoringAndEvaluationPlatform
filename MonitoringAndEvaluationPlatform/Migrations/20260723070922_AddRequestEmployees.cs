using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestEmployees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestEmployeeId",
                table: "Requests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RequestEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestEmployees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Requests_RequestEmployeeId",
                table: "Requests",
                column: "RequestEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_RequestEmployees_RequestEmployeeId",
                table: "Requests",
                column: "RequestEmployeeId",
                principalTable: "RequestEmployees",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_RequestEmployees_RequestEmployeeId",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "RequestEmployees");

            migrationBuilder.DropIndex(
                name: "IX_Requests_RequestEmployeeId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "RequestEmployeeId",
                table: "Requests");
        }
    }
}
