using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class FixRentalOrderClean : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RentalOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),

                    ProductId = table.Column<int>(type: "int", nullable: false),

                    ProductName = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    DailyRentPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),

                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),

                    TotalDays = table.Column<int>(type: "int", nullable: false),

                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),

                    UserEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),

                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),

                    Quantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentalOrders", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentalOrders");
        }
    }
}
