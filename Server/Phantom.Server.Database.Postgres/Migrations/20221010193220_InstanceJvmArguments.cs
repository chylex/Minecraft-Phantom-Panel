using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Phantom.Server.Database.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InstanceJvmArguments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JvmArguments",
                schema: "agents",
                table: "Instances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JvmArguments",
                schema: "agents",
                table: "Instances");
        }
    }
}
