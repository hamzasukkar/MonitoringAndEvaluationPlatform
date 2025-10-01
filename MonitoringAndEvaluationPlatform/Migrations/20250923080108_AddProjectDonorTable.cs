using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDonorTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDonors_Donors_DonorsCode",
                table: "ProjectDonors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDonors_Projects_ProjectsProjectID",
                table: "ProjectDonors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectDonors",
                table: "ProjectDonors");

            migrationBuilder.RenameColumn(
                name: "ProjectsProjectID",
                table: "ProjectDonors",
                newName: "ProjectId");

            migrationBuilder.RenameColumn(
                name: "DonorsCode",
                table: "ProjectDonors",
                newName: "DonorCode");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDonors_ProjectsProjectID",
                table: "ProjectDonors",
                newName: "IX_ProjectDonors_ProjectId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProjectDonors",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<decimal>(
                name: "FundingAmount",
                table: "ProjectDonors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FundingPercentage",
                table: "ProjectDonors",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectDonors",
                table: "ProjectDonors",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "DonorProject",
                columns: table => new
                {
                    DonorsCode = table.Column<int>(type: "int", nullable: false),
                    ProjectsProjectID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonorProject", x => new { x.DonorsCode, x.ProjectsProjectID });
                    table.ForeignKey(
                        name: "FK_DonorProject_Donors_DonorsCode",
                        column: x => x.DonorsCode,
                        principalTable: "Donors",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DonorProject_Projects_ProjectsProjectID",
                        column: x => x.ProjectsProjectID,
                        principalTable: "Projects",
                        principalColumn: "ProjectID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDonors_DonorCode",
                table: "ProjectDonors",
                column: "DonorCode");

            migrationBuilder.CreateIndex(
                name: "IX_DonorProject_ProjectsProjectID",
                table: "DonorProject",
                column: "ProjectsProjectID");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDonors_Donors_DonorCode",
                table: "ProjectDonors",
                column: "DonorCode",
                principalTable: "Donors",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDonors_Projects_ProjectId",
                table: "ProjectDonors",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDonors_Donors_DonorCode",
                table: "ProjectDonors");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDonors_Projects_ProjectId",
                table: "ProjectDonors");

            migrationBuilder.DropTable(
                name: "DonorProject");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectDonors",
                table: "ProjectDonors");

            migrationBuilder.DropIndex(
                name: "IX_ProjectDonors_DonorCode",
                table: "ProjectDonors");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProjectDonors");

            migrationBuilder.DropColumn(
                name: "FundingAmount",
                table: "ProjectDonors");

            migrationBuilder.DropColumn(
                name: "FundingPercentage",
                table: "ProjectDonors");

            migrationBuilder.RenameColumn(
                name: "ProjectId",
                table: "ProjectDonors",
                newName: "ProjectsProjectID");

            migrationBuilder.RenameColumn(
                name: "DonorCode",
                table: "ProjectDonors",
                newName: "DonorsCode");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDonors_ProjectId",
                table: "ProjectDonors",
                newName: "IX_ProjectDonors_ProjectsProjectID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectDonors",
                table: "ProjectDonors",
                columns: new[] { "DonorsCode", "ProjectsProjectID" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDonors_Donors_DonorsCode",
                table: "ProjectDonors",
                column: "DonorsCode",
                principalTable: "Donors",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDonors_Projects_ProjectsProjectID",
                table: "ProjectDonors",
                column: "ProjectsProjectID",
                principalTable: "Projects",
                principalColumn: "ProjectID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
