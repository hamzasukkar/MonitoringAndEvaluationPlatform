using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOutputIndicatorWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ImpactIndicators_ImpactIndicatorsId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ProjectOutputs_ProjectOutputsId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectOutputImpactIndicators",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropIndex(
                name: "IX_ProjectOutputImpactIndicators_ProjectOutputsId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.RenameColumn(
                name: "ProjectOutputsId",
                table: "ProjectOutputImpactIndicators",
                newName: "ProjectOutputId");

            migrationBuilder.RenameColumn(
                name: "ImpactIndicatorsId",
                table: "ProjectOutputImpactIndicators",
                newName: "ImpactIndicatorId");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "ProjectOutputImpactIndicators",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // defaultValue: 1 (not 0) so the existing links this migration is reshaping come out
            // at an equal, valid weight — a stored 0 would violate ProjectOutputImpactIndicator's
            // own [Range(0.01, ...)] and, worse, silently zero out an old link's contribution to
            // WeightedAchievementRate the moment any sibling link on the same output got a real
            // nonzero weight (the equal-weight fallback in ProjectOutput.WeightedAchievementRate
            // only kicks in when EVERY link's weight sums to <= 0, not per-link).
            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "ProjectOutputImpactIndicators",
                type: "float",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectOutputImpactIndicators",
                table: "ProjectOutputImpactIndicators",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputImpactIndicators_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators",
                column: "ImpactIndicatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputImpactIndicators_ProjectOutputId_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators",
                columns: new[] { "ProjectOutputId", "ImpactIndicatorId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ImpactIndicators_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators",
                column: "ImpactIndicatorId",
                principalTable: "ImpactIndicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ProjectOutputs_ProjectOutputId",
                table: "ProjectOutputImpactIndicators",
                column: "ProjectOutputId",
                principalTable: "ProjectOutputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ImpactIndicators_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ProjectOutputs_ProjectOutputId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectOutputImpactIndicators",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropIndex(
                name: "IX_ProjectOutputImpactIndicators_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropIndex(
                name: "IX_ProjectOutputImpactIndicators_ProjectOutputId_ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ProjectOutputImpactIndicators");

            migrationBuilder.RenameColumn(
                name: "ProjectOutputId",
                table: "ProjectOutputImpactIndicators",
                newName: "ProjectOutputsId");

            migrationBuilder.RenameColumn(
                name: "ImpactIndicatorId",
                table: "ProjectOutputImpactIndicators",
                newName: "ImpactIndicatorsId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectOutputImpactIndicators",
                table: "ProjectOutputImpactIndicators",
                columns: new[] { "ImpactIndicatorsId", "ProjectOutputsId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutputImpactIndicators_ProjectOutputsId",
                table: "ProjectOutputImpactIndicators",
                column: "ProjectOutputsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ImpactIndicators_ImpactIndicatorsId",
                table: "ProjectOutputImpactIndicators",
                column: "ImpactIndicatorsId",
                principalTable: "ImpactIndicators",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectOutputImpactIndicators_ProjectOutputs_ProjectOutputsId",
                table: "ProjectOutputImpactIndicators",
                column: "ProjectOutputsId",
                principalTable: "ProjectOutputs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
