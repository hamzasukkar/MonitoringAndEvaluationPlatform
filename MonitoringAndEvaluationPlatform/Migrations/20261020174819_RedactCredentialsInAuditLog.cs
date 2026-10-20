using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoringAndEvaluationPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RedactCredentialsInAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AuditInterceptor used to record every changed column verbatim, so AuditLogs holds
            // user password hashes and security stamps, and the authenticator keys and recovery
            // codes that Identity keeps in AspNetUserTokens.Value. The interceptor now writes
            // "[redacted]" for those; this clears what was recorded before.
            //
            // JSON_VALUE sits inside a CASE so it never sees a non-JSON row: SQL Server does not
            // promise to evaluate ISJSON first in a plain AND, and JSON_VALUE fails on invalid
            // JSON. The IS NOT NULL test also stops JSON_MODIFY (lax mode) from adding a key
            // that was never there.
            var targets = new[]
            {
                (Entity: "= N'ApplicationUser'", Path: "$.PasswordHash"),
                (Entity: "= N'ApplicationUser'", Path: "$.SecurityStamp"),
                (Entity: "LIKE N'IdentityUserToken%'", Path: "$.Value"),
            };

            foreach (var column in new[] { "OldValues", "NewValues" })
            {
                foreach (var (entity, path) in targets)
                {
                    migrationBuilder.Sql($@"
UPDATE [AuditLogs]
SET [{column}] = JSON_MODIFY([{column}], '{path}', N'[redacted]')
WHERE [EntityName] {entity}
  AND (CASE WHEN ISJSON([{column}]) = 1 THEN JSON_VALUE([{column}], '{path}') END) IS NOT NULL
  AND (CASE WHEN ISJSON([{column}]) = 1 THEN JSON_VALUE([{column}], '{path}') END) <> N'[redacted]';");
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the redacted values cannot be recovered, and must not be.
        }
    }
}
