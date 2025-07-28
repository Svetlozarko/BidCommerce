using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidCommerce.Migrations
{
    /// <inheritdoc />
    public partial class FollowersCOunt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Followers",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Followers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId1",
                table: "Followers",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Followers_ApplicationUserId",
                table: "Followers",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Followers_ApplicationUserId1",
                table: "Followers",
                column: "ApplicationUserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_ApplicationUserId",
                table: "Followers",
                column: "ApplicationUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Followers_Users_ApplicationUserId1",
                table: "Followers",
                column: "ApplicationUserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Followers_Users_ApplicationUserId",
                table: "Followers");

            migrationBuilder.DropForeignKey(
                name: "FK_Followers_Users_ApplicationUserId1",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_ApplicationUserId",
                table: "Followers");

            migrationBuilder.DropIndex(
                name: "IX_Followers_ApplicationUserId1",
                table: "Followers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Followers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId1",
                table: "Followers");

            migrationBuilder.AddColumn<int>(
                name: "Followers",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
