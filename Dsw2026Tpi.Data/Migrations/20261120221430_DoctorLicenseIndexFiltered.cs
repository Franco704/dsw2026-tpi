using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class DoctorLicenseIndexFiltered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctors_license_number",
                table: "Doctors");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_license_number",
                table: "Doctors",
                column: "license_number",
                unique: true,
                filter: "[deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Doctors_license_number",
                table: "Doctors");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_license_number",
                table: "Doctors",
                column: "license_number",
                unique: true);
        }
    }
}
