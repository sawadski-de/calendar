using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentStatusAndAttendees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_all_day",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "appointments",
                type: "text",
                nullable: false,
                defaultValue: "Unterbrechbar");

            migrationBuilder.CreateTable(
                name: "attendees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    appointment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendees", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendees_appointments_appointment_id",
                        column: x => x.appointment_id,
                        principalTable: "appointments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attendees_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendees_appointment_id_person_id",
                table: "attendees",
                columns: new[] { "appointment_id", "person_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendees_person_id",
                table: "attendees",
                column: "person_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendees");

            migrationBuilder.DropColumn(
                name: "is_all_day",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "status",
                table: "appointments");
        }
    }
}
