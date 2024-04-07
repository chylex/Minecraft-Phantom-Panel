using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Controller.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class UserAgentAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserAgentAccess",
                schema: "identity",
                columns: table => new
                {
                    UserGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    AgentGuid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAgentAccess", x => new { x.UserGuid, x.AgentGuid });
                    table.ForeignKey(
                        name: "FK_UserAgentAccess_Agents_AgentGuid",
                        column: x => x.AgentGuid,
                        principalSchema: "agents",
                        principalTable: "Agents",
                        principalColumn: "AgentGuid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAgentAccess_Users_UserGuid",
                        column: x => x.UserGuid,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "UserGuid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAgentAccess_AgentGuid",
                schema: "identity",
                table: "UserAgentAccess",
                column: "AgentGuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAgentAccess",
                schema: "identity");
        }
    }
}
