using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("Interests");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(i => i.Slug)
            .IsUnique();

        builder.Property(i => i.Description)
            .HasMaxLength(300);

        builder.Property(i => i.Icon)
            .HasMaxLength(50);

        builder.Property(i => i.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(i => i.CreatedAtUtc)
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc)
            .IsRequired();

        // Seed 16 chủ đề chuẩn ban đầu
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000101"),
                Name = "Công nghệ",
                Slug = "technology",
                Description = "Tin tức, phần cứng, tiện ích và xu hướng công nghệ mới nhất",
                Icon = "💻",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                Name = "Lập trình",
                Slug = "programming",
                Description = "Phát triển phần mềm, thuật toán, web, mobile và hệ thống",
                Icon = "👨‍💻",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000103"),
                Name = "Trí tuệ nhân tạo (AI)",
                Slug = "ai",
                Description = "Machine Learning, Deep Learning, Generative AI và công nghệ tương lai",
                Icon = "🤖",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000104"),
                Name = "Trò chơi (Gaming)",
                Slug = "gaming",
                Description = "Game PC, Console, Mobile, Esports và tin tức làng game",
                Icon = "🎮",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000105"),
                Name = "Thể thao & Bóng đá",
                Slug = "sports",
                Description = "Bóng đá đỉnh cao, tin tức thể thao và giải đấu trong nước quốc tế",
                Icon = "⚽",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000106"),
                Name = "Âm nhạc",
                Slug = "music",
                Description = "Giai điệu hot, nghệ sĩ, concert, nhạc cụ và các bản hit mới",
                Icon = "🎵",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000107"),
                Name = "Phim ảnh & Điện ảnh",
                Slug = "movies",
                Description = "Bom tấn chiếu rạp, series truyền hình, review phim và điện ảnh",
                Icon = "🎬",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000108"),
                Name = "Thiết kế & Nghệ thuật",
                Slug = "design",
                Description = "UI/UX, đồ họa, typography, kiến trúc và sáng tạo thị giác",
                Icon = "🎨",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000109"),
                Name = "Kinh doanh & Khởi nghiệp",
                Slug = "business",
                Description = "Tài chính, startup, marketing, kinh tế số và đầu tư",
                Icon = "💼",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000110"),
                Name = "Khoa học & Vũ trụ",
                Slug = "science",
                Description = "Khám phá khoa học, thiên văn vũ trụ, vật lý và tự nhiên",
                Icon = "🔬",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000111"),
                Name = "Du lịch & Trải nghiệm",
                Slug = "travel",
                Description = "Điểm đến hấp dẫn, cẩm nang phượt, phong cảnh và khám phá thế giới",
                Icon = "✈️",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000112"),
                Name = "Ẩm thực & Nấu ăn",
                Slug = "food",
                Description = "Món ngon mỗi ngày, công thức nấu ăn ngon và review ẩm thực",
                Icon = "🍜",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000113"),
                Name = "Sức khỏe & Fitness",
                Slug = "fitness",
                Description = "Tập luyện gym, yoga, dinh dưỡng khoa học và phong cách sống khỏe",
                Icon = "🏋️",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000114"),
                Name = "Nhiếp ảnh",
                Slug = "photography",
                Description = "Kỹ thuật chụp ảnh, máy ảnh, màu sắc và khoảnh khắc cuộc sống",
                Icon = "📷",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000115"),
                Name = "Anime & Manga",
                Slug = "anime",
                Description = "Văn hóa truyện tranh, anime Nhật Bản, cosplay và cộng đồng fan",
                Icon = "⛩️",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            },
            new Interest
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000116"),
                Name = "Sách & Tri thức",
                Slug = "books",
                Description = "Review sách, thói quen đọc, phát triển tư duy và văn học",
                Icon = "📚",
                IsActive = true,
                CreatedAtUtc = seedDate,
                UpdatedAtUtc = seedDate
            }
        );
    }
}
