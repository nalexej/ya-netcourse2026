using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventMgtApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedAnonymousUserAndMigrateBookings : Migration
    {
        private static readonly Guid AnonymousUserId = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        private static readonly string AnonymousUserPasswordHash = Guid.NewGuid().ToString();

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Вставляем анонимного пользователя
            migrationBuilder.Sql($@"
                INSERT INTO ""users"" (""id"", ""login"", ""password_hash"", ""role"")
                VALUES (
                    '{AnonymousUserId}'::uuid,
                    'anonymous',
                    '{AnonymousUserPasswordHash}'::text, 
                    'User'
                )
                ON CONFLICT (""id"") DO NOTHING;
            ");


            // 2. Привязываем старые брони к анонимному пользователю
            migrationBuilder.Sql($@"
                UPDATE ""bookings"" 
                SET ""user_id"" = '{AnonymousUserId}'::uuid
                WHERE ""user_id"" IS NULL;
            ");

            // 5. Делаем колонку NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "bookings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат: убираем привязку к анониму и удаляем пользователя

            // Делаем колонку NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "bookings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            migrationBuilder.Sql($@"
                UPDATE ""bookings"" 
                SET ""user_id"" = NULL
                WHERE ""user_id"" = '{AnonymousUserId}'::uuid;
            ");

            migrationBuilder.Sql($@"
                DELETE FROM ""users"" WHERE ""id"" = '{AnonymousUserId}'::uuid;
            ");
        }
    }
}
