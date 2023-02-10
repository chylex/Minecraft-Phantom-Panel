using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class EventLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLog",
                schema: "system",
                columns: table => new
                {
                    EventGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    UtcTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AgentGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    SubjectType = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLog", x => x.EventGuid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLog",
                schema: "system");
        }
    }
}
