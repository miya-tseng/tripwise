using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: false),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: false),
                    BookingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotNeeded"),
                    ConfirmationCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, defaultValue: ""),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    CostCurrency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false, defaultValue: "TWD"),
                    PlaceAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PlaceGoogleMapsUri = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PlaceLatitude = table.Column<double>(type: "double precision", nullable: true),
                    PlaceLongitude = table.Column<double>(type: "double precision", nullable: true),
                    PlaceName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PlaceId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stays_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stays_TripId_CheckIn",
                table: "Stays",
                columns: new[] { "TripId", "CheckIn" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stays");
        }
    }
}
