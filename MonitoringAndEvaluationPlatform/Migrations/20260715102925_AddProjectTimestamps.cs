using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // Backfill from audit history so initial ordering reflects real edit times;
            // fall back to StartDate for projects with no audit entries
            migrationBuilder.Sql(@"
UPDATE p SET LastModifiedAt = ISNULL(
    (SELECT MAX(a.[Timestamp]) FROM AuditLogs a
     WHERE a.EntityName = 'Project' AND a.EntityId = CAST(p.ProjectID AS nvarchar(100))),
    p.StartDate)
FROM Projects p;

UPDATE p SET CreatedAt = ISNULL(
    (SELECT MIN(a.[Timestamp]) FROM AuditLogs a
     WHERE a.EntityName = 'Project' AND a.EntityId = CAST(p.ProjectID AS nvarchar(100))),
    p.StartDate)
FROM Projects p;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Projects");
        }
    }
}
