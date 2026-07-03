using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarConnectionsAndExternalAttendees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "attendees",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "external_display_name",
                table: "attendees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_email",
                table: "attendees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                table: "appointments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "calendar_connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    encrypted_access_token = table.Column<string>(type: "text", nullable: true),
                    encrypted_refresh_token = table.Column<string>(type: "text", nullable: true),
                    access_token_expires_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_successful_sync_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    consecutive_failure_count = table.Column<int>(type: "integer", nullable: false),
                    last_error_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_connections", x => x.id);
                    table.ForeignKey(
                        name: "fk_calendar_connections_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_attendees_internal_xor_external",
                table: "attendees",
                sql: "(person_id IS NOT NULL) <> (external_email IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_calendar_connections_person_id_provider",
                table: "calendar_connections",
                columns: new[] { "person_id", "provider" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_connections");

            migrationBuilder.DropCheckConstraint(
                name: "ck_attendees_internal_xor_external",
                table: "attendees");

            migrationBuilder.DropColumn(
                name: "external_display_name",
                table: "attendees");

            migrationBuilder.DropColumn(
                name: "external_email",
                table: "attendees");

            migrationBuilder.DropColumn(
                name: "location",
                table: "appointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "person_id",
                table: "attendees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
