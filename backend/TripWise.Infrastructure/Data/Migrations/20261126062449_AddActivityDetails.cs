using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingStatus",
                table: "Activities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotNeeded");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Activities",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.AddColumn<string>(
                name: "ConfirmationCode",
                table: "Activities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Link",
                table: "Activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Activities",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingStatus",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ConfirmationCode",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Link",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Activities");
        }
    }
}
