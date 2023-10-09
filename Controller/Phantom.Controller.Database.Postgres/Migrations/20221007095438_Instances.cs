using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Controller.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class Instances : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Instances",
                schema: "agents",
                columns: table => new
                {
                    InstanceGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceName = table.Column<string>(type: "text", nullable: false),
                    ServerPort = table.Column<int>(type: "integer", nullable: false),
                    RconPort = table.Column<int>(type: "integer", nullable: false),
                    MinecraftVersion = table.Column<string>(type: "text", nullable: false),
                    MinecraftServerKind = table.Column<string>(type: "text", nullable: false),
                    MemoryAllocation = table.Column<int>(type: "integer", nullable: false),
                    JavaRuntimeGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    LaunchAutomatically = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.InstanceGuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Instances",
                schema: "agents");
        }
    }
}
