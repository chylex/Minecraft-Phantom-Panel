using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Migrations
{
    public partial class Dev2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "agents",
                table: "Agents",
                newName: "AgentId");

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
                    MemoryAllocation = table.Column<ushort>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instances", x => x.InstanceGuid);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Instances",
                schema: "agents");

            migrationBuilder.RenameColumn(
                name: "AgentId",
                schema: "agents",
                table: "Agents",
                newName: "Id");
        }
    }
}
