using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMgtApi.Migrations
{
    /// <inheritdoc />
    public partial class AlterBookingUserIdFieldName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "bookings",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_UserId",
                table: "bookings",
                newName: "IX_bookings_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "bookings",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_bookings_user_id",
                table: "bookings",
                newName: "IX_bookings_UserId");
        }
    }
}
