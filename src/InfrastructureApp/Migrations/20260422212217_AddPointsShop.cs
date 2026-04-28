using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InfrastructureApp.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShopItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CostPoints = table.Column<int>(type: "int", nullable: false),
                    IsSinglePurchase = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserShopItemPurchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ShopItemId = table.Column<int>(type: "int", nullable: false),
                    CostPoints = table.Column<int>(type: "int", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserShopItemPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserShopItemPurchases_ShopItems_ShopItemId",
                        column: x => x.ShopItemId,
                        principalTable: "ShopItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopItems_Name",
                table: "ShopItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserShopItemPurchases_ShopItemId",
                table: "UserShopItemPurchases",
                column: "ShopItemId");

            migrationBuilder.CreateIndex(
                name: "IX_UserShopItemPurchases_UserId_ShopItemId_PurchasedAt",
                table: "UserShopItemPurchases",
                columns: new[] { "UserId", "ShopItemId", "PurchasedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserShopItemPurchases");

            migrationBuilder.DropTable(
                name: "ShopItems");
        }
    }
}
