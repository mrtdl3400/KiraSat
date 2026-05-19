using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RenytTypeAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyRentPrice",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "RentType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentType",
                table: "Products");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRentPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
