using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveActivityConnectPlansToActionPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plans_Activities_ActivityCode",
                table: "Plans");

            migrationBuilder.DropTable(
                name: "Activities");

            migrationBuilder.RenameColumn(
                name: "ActivityCode",
                table: "Plans",
                newName: "ActionPlanCode");

            migrationBuilder.RenameIndex(
                name: "IX_Plans_ActivityCode",
                table: "Plans",
                newName: "IX_Plans_ActionPlanCode");

            // Data fix: Plans.ActionPlanCode currently holds old Activity.Code values (now orphaned).
            // Since the Activities table has been dropped and the intermediate mapping is lost,
            // delete all existing Plans rows. They will be recreated by the application on next use.
            migrationBuilder.Sql("DELETE FROM Plans");

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_ActionPlans_ActionPlanCode",
                table: "Plans",
                column: "ActionPlanCode",
                principalTable: "ActionPlans",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plans_ActionPlans_ActionPlanCode",
                table: "Plans");

            migrationBuilder.RenameColumn(
                name: "ActionPlanCode",
                table: "Plans",
                newName: "ActivityCode");

            migrationBuilder.RenameIndex(
                name: "IX_Plans_ActionPlanCode",
                table: "Plans",
                newName: "IX_Plans_ActivityCode");

            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Code = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionPlanCode = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Activities_ActionPlans_ActionPlanCode",
                        column: x => x.ActionPlanCode,
                        principalTable: "ActionPlans",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activities_ActionPlanCode",
                table: "Activities",
                column: "ActionPlanCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_Activities_ActivityCode",
                table: "Plans",
                column: "ActivityCode",
                principalTable: "Activities",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
