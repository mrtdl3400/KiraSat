using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    [Migration("20260625150000_AddRentalStockCount")]
    public partial class AddRentalStockCount : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Products', 'RentalStockCount') IS NULL
BEGIN
    ALTER TABLE Products
    ADD RentalStockCount int NOT NULL
        CONSTRAINT DF_Products_RentalStockCount DEFAULT(0);

    EXEC('UPDATE Products
          SET RentalStockCount = StockCount
          WHERE IsRentable = 1 AND RentalStockCount = 0');
END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Products', 'RentalStockCount') IS NOT NULL
BEGIN
    IF OBJECT_ID('DF_Products_RentalStockCount', 'D') IS NOT NULL
        ALTER TABLE Products DROP CONSTRAINT DF_Products_RentalStockCount;

    ALTER TABLE Products DROP COLUMN RentalStockCount;
END");
        }
    }
}
