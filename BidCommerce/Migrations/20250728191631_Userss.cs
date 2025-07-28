using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidCommerce.Migrations
{
    /// <inheritdoc />
    public partial class Userss : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
