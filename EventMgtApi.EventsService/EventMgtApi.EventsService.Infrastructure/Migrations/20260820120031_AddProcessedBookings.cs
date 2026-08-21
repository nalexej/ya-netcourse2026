using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMgtApi.EventsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessedBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedBookings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedBookings_EventId_BookingId_EventType",
                table: "ProcessedBookings",
                columns: new[] { "EventId", "BookingId", "EventType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedBookings");
        }
    }
}
