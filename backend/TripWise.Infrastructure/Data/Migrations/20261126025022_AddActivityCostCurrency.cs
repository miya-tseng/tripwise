using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TripWise.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityCostCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "Activities",
                type: "character(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "TWD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "Activities");
        }
    }
}
