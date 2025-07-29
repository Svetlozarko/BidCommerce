using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidCommerce.Migrations
{
    /// <inheritdoc />
    public partial class StatusDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_ProductsStatus_StatusId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductsStatus",
                table: "ProductsStatus");

            migrationBuilder.RenameTable(
                name: "ProductsStatus",
                newName: "Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Status",
                table: "Status",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Status_StatusId",
                table: "Products",
                column: "StatusId",
                principalTable: "Status",
                principalColumn: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Status_StatusId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Status",
                table: "Status");

            migrationBuilder.RenameTable(
                name: "Status",
                newName: "ProductsStatus");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductsStatus",
                table: "ProductsStatus",
                column: "StatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_ProductsStatus_StatusId",
                table: "Products",
                column: "StatusId",
                principalTable: "ProductsStatus",
                principalColumn: "StatusId");
        }
    }
}
