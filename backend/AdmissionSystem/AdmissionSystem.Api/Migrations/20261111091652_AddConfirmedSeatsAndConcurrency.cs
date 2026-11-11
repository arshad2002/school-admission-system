using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdmissionSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConfirmedSeatsAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holds_ClassId",
                table: "Holds");

            migrationBuilder.AddColumn<int>(
                name: "ConfirmedSeats",
                table: "Classes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // xmin is a built-in PostgreSQL system column — no need to add it.

            migrationBuilder.CreateIndex(
                name: "IX_Holds_ClassId_StudentId_Status",
                table: "Holds",
                columns: new[] { "ClassId", "StudentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holds_ClassId_StudentId_Status",
                table: "Holds");

            migrationBuilder.DropColumn(
                name: "ConfirmedSeats",
                table: "Classes");

            // xmin is a built-in PostgreSQL system column — no need to drop it.

            migrationBuilder.CreateIndex(
                name: "IX_Holds_ClassId",
                table: "Holds",
                column: "ClassId");
        }
    }
}
