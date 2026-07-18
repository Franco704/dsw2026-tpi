using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dsw2026Tpi.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSpecialitiesIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Specialities_Name",
                table: "Specialities");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_Name",
                table: "Specialities",
                column: "Name",
                unique: true,
                filter: "[Deleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Specialities_Name",
                table: "Specialities");

            migrationBuilder.CreateIndex(
                name: "IX_Specialities_Name",
                table: "Specialities",
                column: "Name",
                unique: true);
        }
    }
}
