using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class add_role_column : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "AppUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "AppUsers");
        }
    }
}
