using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Controller.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AgentAuthSecretNullability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "AuthSecret",
                schema: "agents",
                table: "Agents",
                type: "bytea",
                maxLength: 12,
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldMaxLength: 12,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "AuthSecret",
                schema: "agents",
                table: "Agents",
                type: "bytea",
                maxLength: 12,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldMaxLength: 12);
        }
    }
}
