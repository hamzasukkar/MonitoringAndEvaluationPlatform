using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class SuperVisorProjectManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectManager",
                table: "Program");

            migrationBuilder.DropColumn(
                name: "SuperVisor",
                table: "Program");

            migrationBuilder.AddColumn<int>(
                name: "ProjectManagerCode",
                table: "Program",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SuperVisorCode",
                table: "Program",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProjectManager",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectManager", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "SuperVisor",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuperVisor", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Program_ProjectManagerCode",
                table: "Program",
                column: "ProjectManagerCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_SuperVisorCode",
                table: "Program",
                column: "SuperVisorCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Program_ProjectManager_ProjectManagerCode",
                table: "Program",
                column: "ProjectManagerCode",
                principalTable: "ProjectManager",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Program_SuperVisor_SuperVisorCode",
                table: "Program",
                column: "SuperVisorCode",
                principalTable: "SuperVisor",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Program_ProjectManager_ProjectManagerCode",
                table: "Program");

            migrationBuilder.DropForeignKey(
                name: "FK_Program_SuperVisor_SuperVisorCode",
                table: "Program");

            migrationBuilder.DropTable(
                name: "ProjectManager");

            migrationBuilder.DropTable(
                name: "SuperVisor");

            migrationBuilder.DropIndex(
                name: "IX_Program_ProjectManagerCode",
                table: "Program");

            migrationBuilder.DropIndex(
                name: "IX_Program_SuperVisorCode",
                table: "Program");

            migrationBuilder.DropColumn(
                name: "ProjectManagerCode",
                table: "Program");

            migrationBuilder.DropColumn(
                name: "SuperVisorCode",
                table: "Program");

            migrationBuilder.AddColumn<string>(
                name: "ProjectManager",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SuperVisor",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
