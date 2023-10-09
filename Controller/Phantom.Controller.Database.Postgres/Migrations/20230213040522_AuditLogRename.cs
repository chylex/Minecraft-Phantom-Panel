using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AuditLogRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_Users_UserId",
                schema: "system",
                table: "AuditEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditEvents",
                schema: "system",
                table: "AuditEvents");

            migrationBuilder.RenameTable(
                name: "AuditEvents",
                schema: "system",
                newName: "AuditLog",
                newSchema: "system");

            migrationBuilder.RenameIndex(
                name: "IX_AuditEvents_UserId",
                schema: "system",
                table: "AuditLog",
                newName: "IX_AuditLog_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLog",
                schema: "system",
                table: "AuditLog",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_Users_UserId",
                schema: "system",
                table: "AuditLog",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_Users_UserId",
                schema: "system",
                table: "AuditLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLog",
                schema: "system",
                table: "AuditLog");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                schema: "system",
                newName: "AuditEvents",
                newSchema: "system");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLog_UserId",
                schema: "system",
                table: "AuditEvents",
                newName: "IX_AuditEvents_UserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditEvents",
                schema: "system",
                table: "AuditEvents",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_Users_UserId",
                schema: "system",
                table: "AuditEvents",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
