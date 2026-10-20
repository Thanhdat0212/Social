using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketplaceRecommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalCustomerInfo",
                table: "ShopOrders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "ShopOrders",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ShopOrders",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Marketplace");

            migrationBuilder.CreateTable(
                name: "ProductCoOccurrences",
                columns: table => new
                {
                    ProductAId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductBId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoOccurrenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    Score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCoOccurrences", x => new { x.ProductAId, x.ProductBId });
                    table.ForeignKey(
                        name: "FK_ProductCoOccurrences_Products_ProductAId",
                        column: x => x.ProductAId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductCoOccurrences_Products_ProductBId",
                        column: x => x.ProductBId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueryText = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NormalizedQuery = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserProductEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    SessionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    DwellTimeSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProductEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserProductEvents_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProductEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCoOccurrences_ProductAId_Score",
                table: "ProductCoOccurrences",
                columns: new[] { "ProductAId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductCoOccurrences_ProductBId",
                table: "ProductCoOccurrences",
                column: "ProductBId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_NormalizedQuery",
                table: "SearchHistory",
                column: "NormalizedQuery");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_UserId_CreatedAtUtc",
                table: "SearchHistory",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvents_ProductId_EventType_CreatedAtUtc",
                table: "UserProductEvents",
                columns: new[] { "ProductId", "EventType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvents_SessionId_CreatedAtUtc",
                table: "UserProductEvents",
                columns: new[] { "SessionId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProductEvents_UserId_CreatedAtUtc",
                table: "UserProductEvents",
                columns: new[] { "UserId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductCoOccurrences");

            migrationBuilder.DropTable(
                name: "SearchHistory");

            migrationBuilder.DropTable(
                name: "UserProductEvents");

            migrationBuilder.DropColumn(
                name: "ExternalCustomerInfo",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "ShopOrders");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ShopOrders");
        }
    }
}
