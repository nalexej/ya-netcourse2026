using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMgtApi.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndUserToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Создаём таблицу users
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            // 2. Добавляем колонку UserId (nullable — данные заполним отдельной миграцией)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "bookings",
                type: "uuid",
                nullable: true);

            // 3. Индекс по UserId
            migrationBuilder.CreateIndex(
                name: "IX_bookings_UserId",
                table: "bookings",
                column: "UserId");

            // 4. Индекс по логину
            migrationBuilder.CreateIndex(
                name: "IX_users_login",
                table: "users",
                column: "login",
                unique: true);

            // 5. Foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_bookings_users_user_id",
                table: "bookings",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_bookings_users_user_id",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_bookings_UserId",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "bookings");

            migrationBuilder.DropIndex(
                name: "IX_users_login",
                table: "users");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
