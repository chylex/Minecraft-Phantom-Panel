using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Controller.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AgentVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Version",
                schema: "agents",
                table: "Agents",
                newName: "ProtocolVersion");

            migrationBuilder.AddColumn<string>(
                name: "BuildVersion",
                schema: "agents",
                table: "Agents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuildVersion",
                schema: "agents",
                table: "Agents");

            migrationBuilder.RenameColumn(
                name: "ProtocolVersion",
                schema: "agents",
                table: "Agents",
                newName: "Version");
        }
    }
}
