using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            // Flag every EXISTING account. Accounts seeded before this migration all shared
            // well-known passwords ("Ministry@123" across 81 ministry accounts, "Admin@123"
            // for the administrator), so none of them can be trusted. Each user must set a
            // password of their own on next sign-in.
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [MustChangePassword] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PasswordChangedAt",
                table: "AspNetUsers");
        }
    }
}
