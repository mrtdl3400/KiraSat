using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiraSepet.DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddUserReplyToContactMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UserRepliedAt",
                table: "ContactMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserReply",
                table: "ContactMessages",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserRepliedAt",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "UserReply",
                table: "ContactMessages");
        }
    }
}
