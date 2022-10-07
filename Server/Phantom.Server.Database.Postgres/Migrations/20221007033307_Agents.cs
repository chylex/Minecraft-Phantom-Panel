using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Agents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "agents");

            migrationBuilder.CreateTable(
                name: "Agents",
                schema: "agents",
                columns: table => new
                {
                    AgentGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    MaxInstances = table.Column<int>(type: "integer", nullable: false),
                    MaxMemory = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agents", x => x.AgentGuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Agents",
                schema: "agents");
        }
    }
}
