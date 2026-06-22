using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class UnifyDailyPrice : Migration
    {
        /// <inheritdoc />
        /// 
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
    "UPDATE [Products] SET [DailyPrice] = [DailPrice] " +
    "WHERE [DailyPrice] IS NULL AND [DailPrice] IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "DailPrice",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
