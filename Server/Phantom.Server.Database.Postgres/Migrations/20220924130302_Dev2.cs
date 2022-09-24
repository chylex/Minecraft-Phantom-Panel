using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Migrations
{
    /// <inheritdoc />
    public partial class Dev2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxInstances",
                schema: "agents",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxMemory",
                schema: "agents",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "agents",
                table: "Agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxInstances",
                schema: "agents",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "MaxMemory",
                schema: "agents",
                table: "Agents");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "agents",
                table: "Agents");
        }
    }
}
