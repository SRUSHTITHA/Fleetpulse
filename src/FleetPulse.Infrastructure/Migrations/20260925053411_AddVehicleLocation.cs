using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "vehicles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "vehicles",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "vehicles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "vehicles");
        }
    }
}
