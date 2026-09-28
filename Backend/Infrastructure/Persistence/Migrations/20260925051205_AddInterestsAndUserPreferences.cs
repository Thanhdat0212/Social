using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestsAndUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInterests",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInterests", x => new { x.UserId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_UserInterests_Interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInterests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => new { x.UserId, x.InterestId });
                    table.ForeignKey(
                        name: "FK_UserPreferences_Interests_InterestId",
                        column: x => x.InterestId,
                        principalTable: "Interests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Interests",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000101"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tin tức, phần cứng, tiện ích và xu hướng công nghệ mới nhất", "💻", true, "Công nghệ", "technology", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000102"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Phát triển phần mềm, thuật toán, web, mobile và hệ thống", "👨‍💻", true, "Lập trình", "programming", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000103"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Machine Learning, Deep Learning, Generative AI và công nghệ tương lai", "🤖", true, "Trí tuệ nhân tạo (AI)", "ai", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000104"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Game PC, Console, Mobile, Esports và tin tức làng game", "🎮", true, "Trò chơi (Gaming)", "gaming", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000105"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bóng đá đỉnh cao, tin tức thể thao và giải đấu trong nước quốc tế", "⚽", true, "Thể thao & Bóng đá", "sports", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000106"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Giai điệu hot, nghệ sĩ, concert, nhạc cụ và các bản hit mới", "🎵", true, "Âm nhạc", "music", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000107"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bom tấn chiếu rạp, series truyền hình, review phim và điện ảnh", "🎬", true, "Phim ảnh & Điện ảnh", "movies", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000108"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "UI/UX, đồ họa, typography, kiến trúc và sáng tạo thị giác", "🎨", true, "Thiết kế & Nghệ thuật", "design", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000109"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tài chính, startup, marketing, kinh tế số và đầu tư", "💼", true, "Kinh doanh & Khởi nghiệp", "business", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000110"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Khám phá khoa học, thiên văn vũ trụ, vật lý và tự nhiên", "🔬", true, "Khoa học & Vũ trụ", "science", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Điểm đến hấp dẫn, cẩm nang phượt, phong cảnh và khám phá thế giới", "✈️", true, "Du lịch & Trải nghiệm", "travel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000112"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Món ngon mỗi ngày, công thức nấu ăn ngon và review ẩm thực", "🍜", true, "Ẩm thực & Nấu ăn", "food", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000113"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Tập luyện gym, yoga, dinh dưỡng khoa học và phong cách sống khỏe", "🏋️", true, "Sức khỏe & Fitness", "fitness", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000114"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Kỹ thuật chụp ảnh, máy ảnh, màu sắc và khoảnh khắc cuộc sống", "📷", true, "Nhiếp ảnh", "photography", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000115"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Văn hóa truyện tranh, anime Nhật Bản, cosplay và cộng đồng fan", "⛩️", true, "Anime & Manga", "anime", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000116"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Review sách, thói quen đọc, phát triển tư duy và văn học", "📚", true, "Sách & Tri thức", "books", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Interests_Slug",
                table: "Interests",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserInterests_InterestId",
                table: "UserInterests",
                column: "InterestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_InterestId",
                table: "UserPreferences",
                column: "InterestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInterests");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropTable(
                name: "Interests");
        }
    }
}
