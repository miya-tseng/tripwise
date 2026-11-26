using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityPlace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlaceAddress",
                table: "Activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceGoogleMapsUri",
                table: "Activities",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceId",
                table: "Activities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PlaceLatitude",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PlaceLongitude",
                table: "Activities",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceName",
                table: "Activities",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlaceAddress",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlaceGoogleMapsUri",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlaceLatitude",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlaceLongitude",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "PlaceName",
                table: "Activities");
        }
    }
}
