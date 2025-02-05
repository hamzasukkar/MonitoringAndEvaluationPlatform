using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class Edit_Program : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Donor",
                table: "Program");

            migrationBuilder.AlterColumn<string>(
                name: "Status2",
                table: "Program",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status1",
                table: "Program",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Program",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DonorCode",
                table: "Program",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinistrieCode",
                table: "Program",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Program_DonorCode",
                table: "Program",
                column: "DonorCode");

            migrationBuilder.CreateIndex(
                name: "IX_Program_MinistrieCode",
                table: "Program",
                column: "MinistrieCode");

            migrationBuilder.AddForeignKey(
                name: "FK_Program_Donor_DonorCode",
                table: "Program",
                column: "DonorCode",
                principalTable: "Donor",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Program_Ministrie_MinistrieCode",
                table: "Program",
                column: "MinistrieCode",
                principalTable: "Ministrie",
                principalColumn: "Code",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Program_Donor_DonorCode",
                table: "Program");

            migrationBuilder.DropForeignKey(
                name: "FK_Program_Ministrie_MinistrieCode",
                table: "Program");

            migrationBuilder.DropIndex(
                name: "IX_Program_DonorCode",
                table: "Program");

            migrationBuilder.DropIndex(
                name: "IX_Program_MinistrieCode",
                table: "Program");

            migrationBuilder.DropColumn(
                name: "DonorCode",
                table: "Program");

            migrationBuilder.DropColumn(
                name: "MinistrieCode",
                table: "Program");

            migrationBuilder.AlterColumn<string>(
                name: "Status2",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status1",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Donor",
                table: "Program",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
