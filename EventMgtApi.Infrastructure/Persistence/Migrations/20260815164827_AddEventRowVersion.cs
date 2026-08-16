using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMgtApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "events",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "row_version",
                table: "events");
        }
    }
}
