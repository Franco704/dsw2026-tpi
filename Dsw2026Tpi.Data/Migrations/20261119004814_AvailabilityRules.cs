using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AvailabilityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "availability_rule_id",
                table: "Availabilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AvailabilityRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    year = table.Column<short>(type: "smallint", nullable: false),
                    month = table.Column<byte>(type: "tinyint", nullable: false),
                    day_of_week = table.Column<byte>(type: "tinyint", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    deleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AvailabilityRules", x => x.Id);
                    table.CheckConstraint("CK_AvailabilityRules_EndTimeAfterStartTime", "[end_time] > [start_time]");
                    table.ForeignKey(
                        name: "FK_AvailabilityRules_Doctors_doctor_id",
                        column: x => x.doctor_id,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_availability_rule_id",
                table: "Availabilities",
                column: "availability_rule_id");

            migrationBuilder.CreateIndex(
                name: "IX_AvailabilityRules_doctor_id_year_month_day_of_week_start_time_end_time",
                table: "AvailabilityRules",
                columns: new[] { "doctor_id", "year", "month", "day_of_week", "start_time", "end_time" },
                unique: true,
                filter: "[deleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_Availabilities_AvailabilityRules_availability_rule_id",
                table: "Availabilities",
                column: "availability_rule_id",
                principalTable: "AvailabilityRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Availabilities_AvailabilityRules_availability_rule_id",
                table: "Availabilities");

            migrationBuilder.DropTable(
                name: "AvailabilityRules");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_availability_rule_id",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "availability_rule_id",
                table: "Availabilities");
        }
    }
}
