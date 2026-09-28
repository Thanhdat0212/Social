using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Persistence;

public static class DatabaseSeeder
{
    // Mật khẩu mặc định: Password123@
    private const string DefaultPasswordHash = "AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==";

    public static async Task SeedAsync(SocialDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        try
        {
            var postCount = await context.Posts.CountAsync(cancellationToken);
            if (postCount >= 25)
            {
                logger.LogInformation("Database đã có {Count} bài viết, bỏ qua bước nạp dữ liệu mẫu.", postCount);
                return;
            }

            logger.LogInformation("Bắt đầu nạp bộ dữ liệu mẫu phong phú (Người dùng, Bài viết, Tương tác)...");

            // 1. TÀI KHOẢN NGƯỜI DÙNG (12 Chuyên gia đa lĩnh vực)
            var users = GetSeedUsers();
            foreach (var user in users)
            {
                var existing = await context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
                if (existing == null)
                {
                    context.Users.Add(user);
                }
                else
                {
                    existing.DisplayName = user.DisplayName;
                    existing.Bio = user.Bio;
                    existing.AvatarUrl = user.AvatarUrl;
                    existing.PasswordHash = user.PasswordHash;
                    existing.EmailConfirmed = true;
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã hoàn tất nạp 12 tài khoản người dùng mẫu.");

            // 2. SỞ THÍCH VÀ TRỌNG SỐ THUẬT TOÁN (UserInterests & UserPreferences)
            var (userInterests, userPreferences) = GetSeedUserInterestsAndPreferences();
            foreach (var ui in userInterests)
            {
                var exists = await context.UserInterests.AnyAsync(x => x.UserId == ui.UserId && x.InterestId == ui.InterestId, cancellationToken);
                if (!exists) context.UserInterests.Add(ui);
            }
            foreach (var up in userPreferences)
            {
                var existingPref = await context.UserPreferences.FirstOrDefaultAsync(x => x.UserId == up.UserId && x.InterestId == up.InterestId, cancellationToken);
                if (existingPref == null)
                {
                    context.UserPreferences.Add(up);
                }
                else
                {
                    existingPref.Score = up.Score;
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            // 3. MẠNG LƯỚI THEO DÕI (UserFollows)
            var follows = GetSeedFollows();
            foreach (var f in follows)
            {
                var exists = await context.UserFollows.AnyAsync(x => x.FollowerId == f.FollowerId && x.FollowingId == f.FollowingId, cancellationToken);
                if (!exists) context.UserFollows.Add(f);
            }
            await context.SaveChangesAsync(cancellationToken);

            // 4. BÀI VIẾT & GẮN THẺ THUẬT TOÁN (28 Bài viết đa dạng đủ 16 chủ đề)
            var (posts, postInterests) = GetSeedPostsAndInterests();
            foreach (var p in posts)
            {
                var existingPost = await context.Posts.FirstOrDefaultAsync(x => x.Id == p.Id, cancellationToken);
                if (existingPost == null)
                {
                    context.Posts.Add(p);
                }
                else
                {
                    existingPost.Content = p.Content;
                    existingPost.MediaUrls = p.MediaUrls;
                    existingPost.LikeCount = p.LikeCount;
                    existingPost.CommentCount = p.CommentCount;
                    existingPost.ViewCount = p.ViewCount;
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            foreach (var pi in postInterests)
            {
                var existingPi = await context.PostInterests.FirstOrDefaultAsync(x => x.PostId == pi.PostId && x.InterestId == pi.InterestId, cancellationToken);
                if (existingPi == null)
                {
                    context.PostInterests.Add(pi);
                }
                else
                {
                    existingPi.Confidence = pi.Confidence;
                }
            }
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã hoàn tất nạp 28 bài viết mẫu chất lượng cao kèm ảnh Unsplash.");

            // 5. BÌNH LUẬN & TRẢ LỜI BÌNH LUẬN (Comments)
            var comments = GetSeedComments();
            foreach (var c in comments)
            {
                var exists = await context.Comments.AnyAsync(x => x.Id == c.Id, cancellationToken);
                if (!exists) context.Comments.Add(c);
            }
            await context.SaveChangesAsync(cancellationToken);

            // 6. LƯỢT THÍCH BÀI VIẾT (PostLikes)
            var likes = GetSeedLikes();
            foreach (var l in likes)
            {
                var exists = await context.PostLikes.AnyAsync(x => x.PostId == l.PostId && x.UserId == l.UserId, cancellationToken);
                if (!exists) context.PostLikes.Add(l);
            }
            await context.SaveChangesAsync(cancellationToken);

            // 7. NHẬT KÝ TƯƠNG TÁC THUẬT TOÁN (UserInteractions for AI Feed)
            var interactions = GetSeedInteractions();
            foreach (var interaction in interactions)
            {
                var exists = await context.UserInteractions.AnyAsync(x => x.Id == interaction.Id, cancellationToken);
                if (!exists) context.UserInteractions.Add(interaction);
            }
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Đã hoàn thành nạp toàn bộ dữ liệu mẫu mở rộng thành công rực rỡ!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra trong quá trình nạp dữ liệu mẫu: {Message}", ex.Message);
        }
    }

    private static List<User> GetSeedUsers()
    {
        var now = DateTime.UtcNow;
        return new List<User>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111101"),
                Email = "nam.tech@social.com",
                NormalizedEmail = "NAM.TECH@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Nguyễn Hoàng Nam",
                Bio = "Senior Software Engineer & AI Researcher. Đam mê chia sẻ kiến thức về Microservices, AI Agents & Cloud Native. 🚀",
                AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111101",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111102"),
                Email = "linh.design@social.com",
                NormalizedEmail = "LINH.DESIGN@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Lê Ngọc Linh",
                Bio = "Product Designer & Visual Artist 🎨. Yêu thích giao diện tối giản, trải nghiệm người dùng mượt mà & Typography.",
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111102",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111103"),
                Email = "tuan.gamer@social.com",
                NormalizedEmail = "TUAN.GAMER@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Trần Quốc Tuấn",
                Bio = "Esports caster & hardcore gamer. Chuyên review các tựa game AAA và phần cứng PC gaming hàng đầu. 🎮🔥",
                AvatarUrl = "https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111103",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111104"),
                Email = "mai.travel@social.com",
                NormalizedEmail = "MAI.TRAVEL@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Hoàng Mai Chi",
                Bio = "Travel Creator & Travel Blogger ✈️. Đi qua 25 tỉnh thành và 8 quốc gia. Chụp ảnh, thưởng thức ẩm thực đường phố và ghi lại khoảnh khắc.",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111104",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111105"),
                Email = "phong.fitness@social.com",
                NormalizedEmail = "PHONG.FITNESS@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Đặng Hải Phong",
                Bio = "Certified Fitness Coach & Nutritionist 🏋️‍♂️. Lan tỏa lối sống năng động, kỷ luật rèn luyện và chế độ ăn uống khoa học.",
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111105",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111106"),
                Email = "huong.foodie@social.com",
                NormalizedEmail = "HUONG.FOODIE@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Phạm Thu Hương",
                Bio = "Home Chef & Food Reviewer 🍜. Yêu ẩm thực Việt Nam truyền thống và sáng tạo những công thức nấu ăn ngon dễ làm mỗi ngày.",
                AvatarUrl = "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111106",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111107"),
                Email = "minh.startup@social.com",
                NormalizedEmail = "MINH.STARTUP@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Vũ Tuấn Minh",
                Bio = "Founder & Tech Angel Investor 💼. Chia sẻ bài học về Product Management, Growth Hacking và xây dựng đội ngũ công nghệ tinh gọn.",
                AvatarUrl = "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111107",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111108"),
                Email = "lan.reader@social.com",
                NormalizedEmail = "LAN.READER@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Trần Ngọc Lan",
                Bio = "Book Reviewer & Podcaster 📚🎙️. Một cuốn sách mỗi tuần. Cùng nhau khám phá tri thức, tâm lý học và nghệ thuật sống an yên.",
                AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111108",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-14),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111109"),
                Email = "long.photo@social.com",
                NormalizedEmail = "LONG.PHOTO@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Đỗ Hoàng Long",
                Bio = "Street & Documentary Photographer 📷. Ghi lại nhịp thở phố thị, ánh sáng và những câu chuyện đời thường qua ống kính 35mm.",
                AvatarUrl = "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111109",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-12),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111110"),
                Email = "quan.science@social.com",
                NormalizedEmail = "QUAN.SCIENCE@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "TS. Phan Anh Quân",
                Bio = "Nghiên cứu sinh Vật lý thiên văn 🔬🌌. Chia sẻ những tiến bộ mới nhất về khoa học vũ trụ, điện toán lượng tử và năng lượng xanh.",
                AvatarUrl = "https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111110",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-12),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "yen.music@social.com",
                NormalizedEmail = "YEN.MUSIC@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Bùi Hải Yến",
                Bio = "Indie Songwriter & Pianist 🎵🎹. Âm nhạc là nơi lưu giữ cảm xúc chân thật nhất. Yêu thích Lo-fi, Acoustic và nhạc không lời.",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111111",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-10),
                UpdatedAtUtc = now
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                Email = "tri.movie@social.com",
                NormalizedEmail = "TRI.MOVIE@SOCIAL.COM",
                PasswordHash = DefaultPasswordHash,
                DisplayName = "Cao Minh Trí",
                Bio = "Nhà phê bình điện ảnh & Đạo diễn hình ảnh 🎬. Đam mê phân tích ngôn ngữ điện ảnh, góc máy và nghệ thuật kể chuyện đỉnh cao.",
                AvatarUrl = "https://images.unsplash.com/photo-1522075469751-3a6694fb2f61?w=400&auto=format&fit=crop&q=80",
                AvatarPublicId = "seed_avatar_11111112",
                EmailConfirmed = true,
                CreatedAtUtc = now.AddDays(-10),
                UpdatedAtUtc = now
            }
        };
    }

    private static (List<UserInterest>, List<UserPreference>) GetSeedUserInterestsAndPreferences()
    {
        var now = DateTime.UtcNow;
        var interests = new List<UserInterest>();
        var prefs = new List<UserPreference>();

        void AddInterestPref(string userId, string interestId, double score)
        {
            var uGuid = Guid.Parse(userId);
            var iGuid = Guid.Parse(interestId);
            interests.Add(new UserInterest { UserId = uGuid, InterestId = iGuid, CreatedAtUtc = now.AddDays(-10) });
            prefs.Add(new UserPreference { UserId = uGuid, InterestId = iGuid, Score = score, CreatedAtUtc = now.AddDays(-10), UpdatedAtUtc = now });
        }

        // Nam (Công nghệ, Lập trình, AI, Khoa học)
        AddInterestPref("11111111-1111-1111-1111-111111111101", "00000000-0000-0000-0000-000000000101", 3.8);
        AddInterestPref("11111111-1111-1111-1111-111111111101", "00000000-0000-0000-0000-000000000102", 4.2);
        AddInterestPref("11111111-1111-1111-1111-111111111101", "00000000-0000-0000-0000-000000000103", 4.5);
        AddInterestPref("11111111-1111-1111-1111-111111111101", "00000000-0000-0000-0000-000000000110", 2.0);

        // Linh (Thiết kế, Công nghệ, Nhiếp ảnh)
        AddInterestPref("11111111-1111-1111-1111-111111111102", "00000000-0000-0000-0000-000000000108", 4.5);
        AddInterestPref("11111111-1111-1111-1111-111111111102", "00000000-0000-0000-0000-000000000101", 3.0);
        AddInterestPref("11111111-1111-1111-1111-111111111102", "00000000-0000-0000-0000-000000000114", 3.5);

        // Tuấn (Game, Anime, Công nghệ)
        AddInterestPref("11111111-1111-1111-1111-111111111103", "00000000-0000-0000-0000-000000000104", 4.8);
        AddInterestPref("11111111-1111-1111-1111-111111111103", "00000000-0000-0000-0000-000000000115", 4.0);
        AddInterestPref("11111111-1111-1111-1111-111111111103", "00000000-0000-0000-0000-000000000101", 2.8);

        // Chi (Du lịch, Nhiếp ảnh, Ẩm thực)
        AddInterestPref("11111111-1111-1111-1111-111111111104", "00000000-0000-0000-0000-000000000111", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111104", "00000000-0000-0000-0000-000000000114", 4.2);
        AddInterestPref("11111111-1111-1111-1111-111111111104", "00000000-0000-0000-0000-000000000112", 3.8);

        // Phong (Fitness, Thể thao, Ẩm thực)
        AddInterestPref("11111111-1111-1111-1111-111111111105", "00000000-0000-0000-0000-000000000113", 4.8);
        AddInterestPref("11111111-1111-1111-1111-111111111105", "00000000-0000-0000-0000-000000000105", 4.0);
        AddInterestPref("11111111-1111-1111-1111-111111111105", "00000000-0000-0000-0000-000000000112", 2.5);

        // Hương (Ẩm thực, Du lịch, Sức khỏe)
        AddInterestPref("11111111-1111-1111-1111-111111111106", "00000000-0000-0000-0000-000000000112", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111106", "00000000-0000-0000-0000-000000000111", 3.2);
        AddInterestPref("11111111-1111-1111-1111-111111111106", "00000000-0000-0000-0000-000000000113", 2.8);

        // Minh (Kinh doanh, Công nghệ, AI)
        AddInterestPref("11111111-1111-1111-1111-111111111107", "00000000-0000-0000-0000-000000000109", 4.8);
        AddInterestPref("11111111-1111-1111-1111-111111111107", "00000000-0000-0000-0000-000000000101", 3.8);
        AddInterestPref("11111111-1111-1111-1111-111111111107", "00000000-0000-0000-0000-000000000103", 3.2);

        // Lan (Sách, Khoa học, Điện ảnh)
        AddInterestPref("11111111-1111-1111-1111-111111111108", "00000000-0000-0000-0000-000000000116", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111108", "00000000-0000-0000-0000-000000000110", 3.5);
        AddInterestPref("11111111-1111-1111-1111-111111111108", "00000000-0000-0000-0000-000000000107", 3.0);

        // Long (Nhiếp ảnh, Du lịch, Thiết kế)
        AddInterestPref("11111111-1111-1111-1111-111111111109", "00000000-0000-0000-0000-000000000114", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111109", "00000000-0000-0000-0000-000000000111", 3.8);
        AddInterestPref("11111111-1111-1111-1111-111111111109", "00000000-0000-0000-0000-000000000108", 3.0);

        // Quân (Khoa học, AI, Công nghệ)
        AddInterestPref("11111111-1111-1111-1111-111111111110", "00000000-0000-0000-0000-000000000110", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111110", "00000000-0000-0000-0000-000000000103", 4.2);
        AddInterestPref("11111111-1111-1111-1111-111111111110", "00000000-0000-0000-0000-000000000101", 3.5);

        // Yến (Âm nhạc, Anime, Nghệ thuật)
        AddInterestPref("11111111-1111-1111-1111-111111111111", "00000000-0000-0000-0000-000000000106", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111111", "00000000-0000-0000-0000-000000000115", 3.9);
        AddInterestPref("11111111-1111-1111-1111-111111111111", "00000000-0000-0000-0000-000000000108", 3.0);

        // Trí (Điện ảnh, Sách, Thiết kế)
        AddInterestPref("11111111-1111-1111-1111-111111111112", "00000000-0000-0000-0000-000000000107", 4.9);
        AddInterestPref("11111111-1111-1111-1111-111111111112", "00000000-0000-0000-0000-000000000116", 3.8);
        AddInterestPref("11111111-1111-1111-1111-111111111112", "00000000-0000-0000-0000-000000000108", 3.2);

        return (interests, prefs);
    }

    private static List<UserFollow> GetSeedFollows()
    {
        var now = DateTime.UtcNow;
        var follows = new List<UserFollow>();

        void Follow(string from, string to) =>
            follows.Add(new UserFollow
            {
                FollowerId = Guid.Parse(from),
                FollowingId = Guid.Parse(to),
                CreatedAtUtc = now.AddDays(-8)
            });

        Follow("11111111-1111-1111-1111-111111111101", "11111111-1111-1111-1111-111111111102");
        Follow("11111111-1111-1111-1111-111111111101", "11111111-1111-1111-1111-111111111107");
        Follow("11111111-1111-1111-1111-111111111101", "11111111-1111-1111-1111-111111111110");
        Follow("11111111-1111-1111-1111-111111111102", "11111111-1111-1111-1111-111111111101");
        Follow("11111111-1111-1111-1111-111111111102", "11111111-1111-1111-1111-111111111109");
        Follow("11111111-1111-1111-1111-111111111103", "11111111-1111-1111-1111-111111111101");
        Follow("11111111-1111-1111-1111-111111111103", "11111111-1111-1111-1111-111111111111");
        Follow("11111111-1111-1111-1111-111111111104", "11111111-1111-1111-1111-111111111106");
        Follow("11111111-1111-1111-1111-111111111104", "11111111-1111-1111-1111-111111111109");
        Follow("11111111-1111-1111-1111-111111111105", "11111111-1111-1111-1111-111111111104");
        Follow("11111111-1111-1111-1111-111111111106", "11111111-1111-1111-1111-111111111104");
        Follow("11111111-1111-1111-1111-111111111107", "11111111-1111-1111-1111-111111111101");
        Follow("11111111-1111-1111-1111-111111111108", "11111111-1111-1111-1111-111111111107");
        Follow("11111111-1111-1111-1111-111111111109", "11111111-1111-1111-1111-111111111104");
        Follow("11111111-1111-1111-1111-111111111110", "11111111-1111-1111-1111-111111111101");
        Follow("11111111-1111-1111-1111-111111111111", "11111111-1111-1111-1111-111111111102");
        Follow("11111111-1111-1111-1111-111111111112", "11111111-1111-1111-1111-111111111108");

        return follows;
    }

    private static (List<Post>, List<PostInterest>) GetSeedPostsAndInterests()
    {
        var now = DateTime.UtcNow;
        var posts = new List<Post>();
        var postInterests = new List<PostInterest>();

        void AddPost(
            string postId,
            string authorId,
            string content,
            List<string> mediaUrls,
            int likes,
            int comments,
            int views,
            DateTime createdAt,
            params (string interestId, double confidence)[] topics)
        {
            var pGuid = Guid.Parse(postId);
            posts.Add(new Post
            {
                Id = pGuid,
                AuthorId = Guid.Parse(authorId),
                Content = content,
                MediaUrls = mediaUrls,
                Status = PostStatus.Published,
                LikeCount = likes,
                CommentCount = comments,
                ViewCount = views,
                CreatedAtUtc = createdAt,
                UpdatedAtUtc = createdAt
            });

            foreach (var (iId, conf) in topics)
            {
                postInterests.Add(new PostInterest
                {
                    PostId = pGuid,
                    InterestId = Guid.Parse(iId),
                    Confidence = conf,
                    CreatedAtUtc = createdAt
                });
            }
        }

        // 1. AI Agents (Công nghệ, AI, Lập trình)
        AddPost("22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111101",
            "🚀 Tổng kết 6 tháng ứng dụng mô hình AI Agent tự hành vào quy trình phát triển phần mềm:\n\n1. Tốc độ code review tăng 45% nhờ tự động phát hiện logic flaws.\n2. Tự động sinh integration test cases bao phủ các edge cases khó.\n3. Documentation luôn được cập nhật đồng bộ với schema DB.\n\nAI không thay thế lập trình viên, nhưng lập trình viên biết khai thác AI hiệu quả sẽ bứt phá rất xa! Anh em nghĩ sao về xu hướng này?",
            new List<string> { "https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1000&auto=format&fit=crop&q=80" },
            72, 6, 420, now.AddHours(-1),
            ("00000000-0000-0000-0000-000000000103", 0.98),
            ("00000000-0000-0000-0000-000000000102", 0.95),
            ("00000000-0000-0000-0000-000000000101", 0.90));

        // 2. Dark Mode UI (Thiết kế, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222102", "11111111-1111-1111-1111-111111111102",
            "✨ Dark Mode không chỉ đơn giản là đổi màu nền sang đen `#000000`!\n\nTrong thiết kế UI hiện đại:\n- Hãy sử dụng dải màu Dark Gray (như `#0a0a0f`, `#12121a`) để mắt không bị gắt.\n- Tạo chiều sâu cho Cards bằng viền Glassmorphism tinh tế `rgba(255, 255, 255, 0.08)` và bóng mờ đa tầng.\n- Giữ độ tương phản văn bản tối thiểu 4.5:1 để đảm bảo tính tiếp cận (Accessibility).\n\nMọi người thích giao diện Dark Mode huyền bí hay Light Mode trong sáng hơn?",
            new List<string> { "https://images.unsplash.com/photo-1507238691740-187a5b1d37b8?w=1000&auto=format&fit=crop&q=80" },
            58, 4, 380, now.AddHours(-2),
            ("00000000-0000-0000-0000-000000000108", 0.97),
            ("00000000-0000-0000-0000-000000000101", 0.85));

        // 3. Wukong Gaming (Gaming, Âm nhạc)
        AddPost("22222222-2222-2222-2222-222222222103", "11111111-1111-1111-1111-111111111103",
            "🎮 Vừa phá đảo Black Myth: Wukong ở chế độ New Game+! Phải công nhận khâu thiết kế boss fight và hiệu ứng đồ họa Unreal Engine 5 đỉnh cao thực sự.\n\nCảnh quan núi non hùng vĩ và âm nhạc mang đậm chất thần thoại phương Đông làm mình nổi da gà nhiều đoạn. Anh em đã ai mở khóa hết toàn bộ vũ khí và pháp bảo ẩn chưa?",
            new List<string> { "https://images.unsplash.com/photo-1542751371-adc38448a05e?w=1000&auto=format&fit=crop&q=80" },
            115, 8, 590, now.AddHours(-3),
            ("00000000-0000-0000-0000-000000000104", 0.99),
            ("00000000-0000-0000-0000-000000000106", 0.75));

        // 4. Hà Giang Travel (Du lịch, Nhiếp ảnh)
        AddPost("22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111104",
            "⛰️ Bình minh trên đỉnh Mã Pí Lèng - Hà Giang sáng nay đẹp như một bức tranh thuỷ mặc!\n\nKhông khí se lạnh 14°C, sương mù vờn quanh các ngọn núi đá vôi tai mèo và dòng sông Nho Quế xanh ngọc bích uốn lượn dưới chân đèo. Cảm giác đứng giữa đất trời bao la thực sự gột rửa mọi áp lực công việc.\n\nMỗi người trẻ nhất định nên đi Hà Giang ít nhất một lần trong đời!",
            new List<string>
            {
                "https://images.unsplash.com/photo-1528127269322-539801943592?w=1000&auto=format&fit=crop&q=80",
                "https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1000&auto=format&fit=crop&q=80"
            },
            152, 9, 680, now.AddHours(-4),
            ("00000000-0000-0000-0000-000000000111", 0.99),
            ("00000000-0000-0000-0000-000000000114", 0.92));

        // 5. Fitness & Tăng cơ (Fitness, Thể thao)
        AddPost("22222222-2222-2222-2222-222222222105", "11111111-1111-1111-1111-111111111105",
            "💪 3 sai lầm phổ biến nhất khiến bạn tập Gym mãi không tăng cơ giảm mỡ:\n\n1. Bỏ qua Progressive Overload: Mỗi tuần không tăng mức tạ hoặc số reps.\n2. Ăn thiếu Protein: Cơ bắp cần tối thiểu 1.6g - 2.0g Protein / kg thể trọng mỗi ngày.\n3. Ngủ ít hơn 7 tiếng: 80% quá trình hồi phục và phát triển cơ bắp diễn ra khi bạn ngủ sâu.\n\nKỷ luật chính là chiếc cầu nối giữa mục tiêu và kết quả thực tế. Hãy bắt đầu từ hôm nay!",
            new List<string> { "https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=1000&auto=format&fit=crop&q=80" },
            92, 5, 510, now.AddHours(-5),
            ("00000000-0000-0000-0000-000000000113", 0.98),
            ("00000000-0000-0000-0000-000000000105", 0.85));

        // 6. Phở bò Hà Nội (Ẩm thực, Du lịch)
        AddPost("22222222-2222-2222-2222-222222222106", "11111111-1111-1111-1111-111111111106",
            "🍜 Bí quyết để có nồi nước dùng phở bò truyền thống thơm phức, trong vắt mà đậm đà:\n\n- Xương ống bò phải ngâm nước muối 2 tiếng và chần qua nước sôi gừng.\n- Nướng vàng thơm hành tím, gừng, hoa hồi, thảo quả và quế trước khi cho vào túi thơm.\n- Ninh liu riu lửa nhỏ từ 8 - 10 tiếng, liên tục vớt bọt và không đậy nắp vung kín.\n\nMùi vị của ký ức và hương thơm nồng nàn của đất trời tụ lại trong một bát phở nóng hổi sáng đầu tuần!",
            new List<string> { "https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=1000&auto=format&fit=crop&q=80" },
            128, 7, 620, now.AddHours(-6),
            ("00000000-0000-0000-0000-000000000112", 0.99),
            ("00000000-0000-0000-0000-000000000111", 0.80));

        // 7. Startup Product Market Fit (Kinh doanh, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222107", "11111111-1111-1111-1111-111111111107",
            "💼 1 bài học xương máu cho các founder công nghệ giai đoạn Early-stage:\n\nĐừng vội xây dựng tính năng phức tạp trước khi tìm được Product-Market Fit (PMF). Hãy nói chuyện với ít nhất 50 khách hàng tiềm năng, tìm ra 'nỗi đau' thực sự của họ và giải quyết nó tốt gấp 10 lần giải pháp hiện tại.\n\nTốc độ học hỏi và thích ứng mới là lợi thế cạnh tranh lớn nhất của một startup tinh gọn!",
            new List<string> { "https://images.unsplash.com/photo-1556761175-5973dc0f32e7?w=1000&auto=format&fit=crop&q=80" },
            85, 6, 450, now.AddHours(-7),
            ("00000000-0000-0000-0000-000000000109", 0.98),
            ("00000000-0000-0000-0000-000000000101", 0.82));

        // 8. Tâm lý học về tiền (Sách, Kinh doanh)
        AddPost("22222222-2222-2222-2222-222222222108", "11111111-1111-1111-1111-111111111108",
            "📚 Cuốn sách 'The Psychology of Money' của Morgan Housel đã thay đổi hoàn toàn tư duy tài chính của mình:\n\n'Quản lý tiền bạc giỏi không phụ thuộc nhiều vào việc bạn thông minh cỡ nào, mà phụ thuộc vào cách bạn hành xử.'\n\nSự giàu có thực sự là những thứ bạn không nhìn thấy: những chiếc xe không mua, những kỳ nghỉ xa hoa không chi tiêu. Tự do tài chính là khả năng thức dậy mỗi sáng và nói: 'Hôm nay tôi có thể làm bất cứ điều gì tôi muốn.'",
            new List<string> { "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=1000&auto=format&fit=crop&q=80" },
            104, 5, 530, now.AddHours(-8),
            ("00000000-0000-0000-0000-000000000116", 0.99),
            ("00000000-0000-0000-0000-000000000109", 0.88));

        // 9. Street Photography Hà Nội (Nhiếp ảnh, Nghệ thuật)
        AddPost("22222222-2222-2222-2222-222222222109", "11111111-1111-1111-1111-111111111109",
            "📷 Đi dạo một vòng hồ Gươm lúc hoàng hôn buông xuống. Ánh nắng vàng óng hắt qua những táng cây cổ thụ, bóng chiếc xe đạp chở hoa rực rỡ len qua dòng người hối hả.\n\nNhiếp ảnh đường phố không cần máy ảnh đắt tiền, chỉ cần bạn chịu chậm lại một nhịp để cảm nhận hơi thở của cuộc sống.",
            new List<string> { "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=1000&auto=format&fit=crop&q=80" },
            110, 8, 560, now.AddHours(-9),
            ("00000000-0000-0000-0000-000000000114", 0.99),
            ("00000000-0000-0000-0000-000000000108", 0.85));

        // 10. Kính James Webb & Vũ trụ (Khoa học, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222110", "11111111-1111-1111-1111-111111111110",
            "🔬 Hình ảnh trường sâu mới nhất từ Kính viễn vọng Không gian James Webb (JWST) đã hé lộ những thiên hà hình thành chỉ 300 triệu năm sau vụ nổ Big Bang!\n\nÁnh sáng đã di chuyển hơn 13.4 tỷ năm để chạm tới thấu kính hồng ngoại của nhân loại. Mỗi chấm sáng li ti trong bức ảnh này là cả một thiên hà chứa hàng trăm tỷ ngôi sao. Khi ngước nhìn lên vũ trụ, chúng ta đang nhìn ngược về quá khứ.",
            new List<string> { "https://images.unsplash.com/photo-1451187580459-43490279c0fa?w=1000&auto=format&fit=crop&q=80" },
            135, 11, 720, now.AddHours(-10),
            ("00000000-0000-0000-0000-000000000110", 0.99),
            ("00000000-0000-0000-0000-000000000101", 0.85));

        // 11. Indie Music Acoustic (Âm nhạc, Nghệ thuật)
        AddPost("22222222-2222-2222-2222-222222222111", "11111111-1111-1111-1111-111111111111",
            "🎵 Vừa hoàn thiện bản demo một ca khúc Indie mới lúc 2h sáng bên cây đàn guitar mộc mạc.\n\nCó những giai điệu chỉ xuất hiện khi cả thành phố đã chìm vào giấc ngủ. Đôi khi giai điệu đơn sơ nhất lại là giai điệu chạm đến trái tim người nghe sâu sắc nhất. Hy vọng sớm được gửi tới mọi người phiên bản acoustic mộc mạc này!",
            new List<string> { "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?w=1000&auto=format&fit=crop&q=80" },
            95, 7, 480, now.AddHours(-11),
            ("00000000-0000-0000-0000-000000000106", 0.99),
            ("00000000-0000-0000-0000-000000000108", 0.80));

        // 12. Review Dune 2 (Điện ảnh, Nghệ thuật)
        AddPost("22222222-2222-2222-2222-222222222112", "11111111-1111-1111-1111-111111111112",
            "🎬 'Dune: Part Two' xứng đáng là chuẩn mực điện ảnh khoa học viễn tưởng thời đại mới!\n\nĐạo diễn Denis Villeneuve cùng nhà quay phim Greig Fraser đã biến hành tinh cát Arrakis thành một thế giới sống động đến choáng ngợp. Thiết kế âm thanh của Hans Zimmer rung chuyển cả rạp IMAX, kết hợp hoàn hảo cùng diễn xuất đầy nội tâm của Timothée Chalamet. Một trải nghiệm nghệ thuật thị giác và thính giác không tì vết!",
            new List<string> { "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?w=1000&auto=format&fit=crop&q=80" },
            140, 9, 690, now.AddHours(-12),
            ("00000000-0000-0000-0000-000000000107", 0.99),
            ("00000000-0000-0000-0000-000000000108", 0.85));

        // 13. Clean Architecture (Lập trình, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222113", "11111111-1111-1111-1111-111111111101",
            "👨‍💻 Chia sẻ về Clean Architecture trong ứng dụng Backend .NET 8 / 9:\n\n- Tách biệt Domain Layer không phụ thuộc vào bất kỳ framework nào.\n- Application Layer định nghĩa DTOs, Use Cases và Interfaces chuẩn.\n- Infrastructure chịu trách nhiệm triển khai DB Context (EF Core), Redis Cache, 3rd party APIs.\n- Presentation API chỉ lo Routing, Authentication và validation.\n\nCấu trúc này giúp code kiểm thử Unit Test cực kỳ dễ dàng và bảo trì mở rộng lâu dài rất nhàn hạ!",
            new List<string> { "https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=1000&auto=format&fit=crop&q=80" },
            88, 5, 410, now.AddHours(-13),
            ("00000000-0000-0000-0000-000000000102", 0.99),
            ("00000000-0000-0000-0000-000000000101", 0.91));

        // 14. Micro-interactions in UI (Thiết kế, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222114", "11111111-1111-1111-1111-111111111102",
            "🎨 'The details are not the details. They make the design.' — Charles Eames.\n\nMột nút Like có hiệu ứng scale nảy nhẹ khi bấm, một thanh loading gradient êm dịu, hay hiệu ứng chuyển tab mượt mà 200ms... Chính những tương tác vi mô (micro-interactions) này tạo nên cảm giác 'đắt tiền' và thích thú cho người dùng sản phẩm.",
            new List<string> { "https://images.unsplash.com/photo-1581291518857-4e27b48ff24e?w=1000&auto=format&fit=crop&q=80" },
            76, 4, 390, now.AddHours(-14),
            ("00000000-0000-0000-0000-000000000108", 0.98),
            ("00000000-0000-0000-0000-000000000101", 0.80));

        // 15. Gaming Gear & GPU (Gaming, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222115", "11111111-1111-1111-1111-111111111103",
            "🎮 Góc máy chiến game cuối tuần đã hoàn thiện! Dàn PC Custom tản nhiệt nước với case kính panoramic, đèn ARGB sync nhịp nhàng theo nhạc nền.\n\nChơi game ở độ phân giải 2K 144Hz trên tấm nền Fast-IPS mượt như bơ. Anh em khoe góc setup làm việc và giải trí tại gia của mình bên dưới nhé!",
            new List<string> { "https://images.unsplash.com/photo-1598550476439-6847785fcea6?w=1000&auto=format&fit=crop&q=80" },
            120, 10, 610, now.AddHours(-15),
            ("00000000-0000-0000-0000-000000000104", 0.99),
            ("00000000-0000-0000-0000-000000000101", 0.88));

        // 16. Phố cổ Hội An (Du lịch, Ẩm thực, Nhiếp ảnh)
        AddPost("22222222-2222-2222-2222-222222222116", "11111111-1111-1111-1111-111111111104",
            "🏮 Đêm phố cổ Hội An bên dòng sông Hoài thơ mộng. Hàng trăm chiếc đèn lồng lụa đủ màu sắc lung linh phản chiếu xuống mặt nước phẳng lặng.\n\nGhé quán ven đường thưởng thức một tô Cao Lầu dai ngon, nhâm nhi ly trà mót thơm mùi quế sả và hoa sen. Nét trầm mặc cổ kính nơi đây luôn khiến tâm hồn người lữ khách tìm lại sự bình yên.",
            new List<string>
            {
                "https://images.unsplash.com/photo-1559592413-7cec4d0cae2b?w=1000&auto=format&fit=crop&q=80",
                "https://images.unsplash.com/photo-1569154941061-e231b4725ef1?w=1000&auto=format&fit=crop&q=80"
            },
            165, 12, 750, now.AddHours(-16),
            ("00000000-0000-0000-0000-000000000111", 0.99),
            ("00000000-0000-0000-0000-000000000112", 0.88),
            ("00000000-0000-0000-0000-000000000114", 0.85));

        // 17. Chạy bộ Marathon (Thể thao, Sức khỏe)
        AddPost("22222222-2222-2222-2222-222222222117", "11111111-1111-1111-1111-111111111105",
            "🏃‍♂️ Hoàn thành cự ly Half Marathon 21km đầu tiên dưới 2 giờ (Sub 2)! Cảm giác vượt qua giới hạn thể chất của bản thân sau 3 tháng tập luyện kiên trì thực sự bùng nổ hạnh phúc.\n\nChạy bộ dạy chúng ta cách đối thoại với chính mình khi đôi chân mỏi nhừ, tim đập nhanh và tiếng nói trong đầu bảo bỏ cuộc. Cứ kiên trì từng bước một, vạch đích sẽ hiện ra!",
            new List<string> { "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=1000&auto=format&fit=crop&q=80" },
            132, 9, 630, now.AddHours(-17),
            ("00000000-0000-0000-0000-000000000105", 0.99),
            ("00000000-0000-0000-0000-000000000113", 0.95));

        // 18. Mâm cơm mùa thu (Ẩm thực, Nhiếp ảnh)
        AddPost("22222222-2222-2222-2222-222222222118", "11111111-1111-1111-1111-111111111106",
            "🍃 Mâm cơm gia đình giản dị chiều gió mùa về:\n- Chả cốm làng Vòng rán giòn rụm.\n- Canh sườn nấu sấu chua thanh thanh thơm mùi rau ngót.\n- Đĩa thịt ba chỉ luộc chấm mắm tôm kèm cà pháo giòn tan.\n\nSau một ngày bận rộn giữa phố phường, được quây quần bên mâm cơm nóng với người thân là điều ấm áp nhất cuộc đời!",
            new List<string> { "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?w=1000&auto=format&fit=crop&q=80" },
            145, 8, 670, now.AddHours(-18),
            ("00000000-0000-0000-0000-000000000112", 0.99),
            ("00000000-0000-0000-0000-000000000114", 0.78));

        // 19. Văn hóa Doanh nghiệp (Kinh doanh, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222119", "11111111-1111-1111-1111-111111111107",
            "💼 Văn hóa doanh nghiệp không phải là những khẩu hiệu in trên tường, mà là cách đội ngũ đối xử với nhau khi đối mặt với khủng hoảng.\n\nMột môi trường an toàn về tâm lý (psychological safety) cho phép nhân viên dám thử sai, dám nói thẳng sự thật và cùng nhau tìm giải pháp thay vì chỉ trích cá nhân. Đó là nền tảng để một công ty công nghệ đi đường dài.",
            new List<string> { "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?w=1000&auto=format&fit=crop&q=80" },
            89, 6, 460, now.AddHours(-19),
            ("00000000-0000-0000-0000-000000000109", 0.98),
            ("00000000-0000-0000-0000-000000000101", 0.80));

        // 20. Đọc sâu vs Đọc lướt (Sách, Khoa học)
        AddPost("22222222-2222-2222-2222-222222222120", "11111111-1111-1111-1111-111111111108",
            "📚 Bạn có đang rơi vào bẫy 'đọc số lượng thay vì chất lượng'?\n\nTrong thời đại ngập tràn thông tin ngắn, khả năng tập trung đọc sâu (Deep Reading) suốt 1 tiếng đồng hồ mà không chạm vào điện thoại là một siêu năng lực. Hãy chọn một cuốn sách hay, ghi chép lại những luận điểm tâm đắc và quan trọng nhất: áp dụng ít nhất một điều vào cuộc sống mỗi ngày.",
            new List<string> { "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=1000&auto=format&fit=crop&q=80" },
            112, 7, 540, now.AddHours(-20),
            ("00000000-0000-0000-0000-000000000116", 0.99),
            ("00000000-0000-0000-0000-000000000110", 0.82));

        // 21. Săn Ngân Hà Tà Xùa (Nhiếp ảnh, Du lịch)
        AddPost("22222222-2222-2222-2222-222222222121", "11111111-1111-1111-1111-111111111109",
            "🌌 Đêm săn dải ngân hà (Milky Way) trên đỉnh gió Tà Xùa - Sơn La ở độ cao 2.800m.\n\nThời tiết không một gợn mây, nhiệt độ 9°C. Cầm chiếc tripod vững chãi, phơi sáng 25s ở ISO 3200 trên ống kính góc rộng f/1.8. Khi màn trập đóng lại và dải sáng lấp lánh của ngân hà hiện lên màn hình máy ảnh, mọi sự rét buốt đều tan biến!",
            new List<string> { "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?w=1000&auto=format&fit=crop&q=80" },
            175, 14, 820, now.AddHours(-21),
            ("00000000-0000-0000-0000-000000000114", 0.99),
            ("00000000-0000-0000-0000-000000000111", 0.92));

        // 22. Điện toán lượng tử (Khoa học, Công nghệ)
        AddPost("22222222-2222-2222-2222-222222222122", "11111111-1111-1111-1111-111111111110",
            "🔬 Điện toán lượng tử (Quantum Computing) và kỷ nguyên Qubit:\n\nKhác với bit cổ điển chỉ mang giá trị 0 hoặc 1, Qubit khai thác hiện tượng chồng chập lượng tử (Superposition) và rối lượng tử (Entanglement) để xử lý lượng phép tính khổng lồ cùng một lúc. Những bài toán mô phỏng phân tử thuốc hay tối ưu hóa chuỗi cung ứng toàn cầu mất hàng ngàn năm có thể được giải quyết trong vài phút!",
            new List<string> { "https://images.unsplash.com/photo-1635070041078-e363dbe005cb?w=1000&auto=format&fit=crop&q=80" },
            98, 6, 510, now.AddHours(-22),
            ("00000000-0000-0000-0000-000000000110", 0.99),
            ("00000000-0000-0000-0000-000000000101", 0.90));

        // 23. Nhạc phim Anime Piano (Âm nhạc, Anime)
        AddPost("22222222-2222-2222-2222-222222222123", "11111111-1111-1111-1111-111111111111",
            "🎹 Cover bản nhạc không lời 'Sparkle' (Your Name - Makoto Shinkai) trên chiếc piano cơ cổ điển.\n\nÂm hưởng trong trẻo mà da diết của Radwimps đưa ta trở về những giấc mơ thanh xuân dang dở, dưới cơn mưa sao băng rực rỡ lướt ngang bầu trời đêm Tokyo. Mời cả nhà cùng lắng nghe và thư giãn cuối tuần!",
            new List<string> { "https://images.unsplash.com/photo-1520523839898-507125cd53c1?w=1000&auto=format&fit=crop&q=80" },
            128, 8, 640, now.AddHours(-23),
            ("00000000-0000-0000-0000-000000000106", 0.99),
            ("00000000-0000-0000-0000-000000000115", 0.95));

        // 24. Nghệ thuật Kể chuyện Điện ảnh (Điện ảnh, Nghệ thuật)
        AddPost("22222222-2222-2222-2222-222222222124", "11111111-1111-1111-1111-111111111112",
            "🎬 Quy tắc 'Show, Don't Tell' — Linh hồn của ngôn ngữ điện ảnh thực thụ:\n\nĐừng để nhân vật nói 'tôi đang rất buồn', hãy cho khán giả thấy đôi bàn tay run rẩy đánh rơi chiếc tách trà, ánh nhìn vô định ra khung cửa sổ mưa tầm tã và sự im lặng kéo dài nghẹt thở. Nghệ thuật thứ bảy chinh phục trái tim người xem bằng hình ảnh và cảm xúc chân thực.",
            new List<string> { "https://images.unsplash.com/photo-1517604931442-7e0c8ed2963c?w=1000&auto=format&fit=crop&q=80" },
            118, 5, 580, now.AddHours(-24),
            ("00000000-0000-0000-0000-000000000107", 0.99),
            ("00000000-0000-0000-0000-000000000108", 0.88));

        // 25. Anime Mùa Mới (Anime, Gaming)
        AddPost("22222222-2222-2222-2222-222222222125", "11111111-1111-1111-1111-111111111103",
            "⛩️ Mùa Anime thu 2026 chính thức bùng nổ với hàng loạt siêu phẩm trở lại! Chất lượng hoạt họa (Sakuga) của các studio hàng đầu như Ufotable và Mappa tiếp tục nâng chuẩn làm phim hoạt hình lên một nấc thang mới.\n\nCác phân cảnh chiến đấu mượt mà đến từng khung hình. Anh em đang theo dõi bộ nào mùa này nhiều nhất?",
            new List<string> { "https://images.unsplash.com/photo-1578632767115-351597cf2477?w=1000&auto=format&fit=crop&q=80" },
            142, 11, 710, now.AddHours(-25),
            ("00000000-0000-0000-0000-000000000115", 0.99),
            ("00000000-0000-0000-0000-000000000104", 0.85));

        // 26. Giãn cơ Mobility (Sức khỏe, Thể thao)
        AddPost("22222222-2222-2222-2222-222222222126", "11111111-1111-1111-1111-111111111105",
            "🧘‍♂️ 15 phút giãn cơ và Mobility mỗi ngày: Thần dược cho dân văn phòng ngồi nhiều đau lưng mỏi cổ:\n\n- Động tác Cat-Cow kích hoạt toàn bộ đốt sống lưng.\n- Pigeon Pose giải tỏa căng cứng khớp háng và cơ mông.\n- World's Greatest Stretch mở rộng lồng ngực và cải thiện tư thế đứng thẳng.\n\nHãy chăm sóc cơ thể bạn, bởi đó là nơi duy nhất bạn phải sống trong suốt cuộc đời này!",
            new List<string> { "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?w=1000&auto=format&fit=crop&q=80" },
            105, 6, 520, now.AddHours(-26),
            ("00000000-0000-0000-0000-000000000113", 0.99),
            ("00000000-0000-0000-0000-000000000105", 0.89));

        // 27. Chạy Local LLM (AI, Công nghệ, Lập trình)
        AddPost("22222222-2222-2222-2222-222222222127", "11111111-1111-1111-1111-111111111101",
            "🤖 Trải nghiệm chạy mô hình Local LLMs (Ollama / Llama-3 / Gemma) trực tiếp trên chip Apple Silicon và card RTX:\n\n- Tốc độ sinh text đạt hơn 80 tokens/giây.\n- Hoàn toàn offline, bảo mật 100% dữ liệu nhạy cảm không gửi lên cloud.\n- Kết hợp cùng RAG để tra cứu tài liệu kỹ thuật nội bộ siêu chuẩn xác.\n\nKỷ nguyên đưa AI về máy cá nhân đang phát triển nhanh chưa từng thấy!",
            new List<string> { "https://images.unsplash.com/photo-1677442136019-21780ecad995?w=1000&auto=format&fit=crop&q=80" },
            115, 8, 590, now.AddHours(-27),
            ("00000000-0000-0000-0000-000000000103", 0.99),
            ("00000000-0000-0000-0000-000000000101", 0.95),
            ("00000000-0000-0000-0000-000000000102", 0.90));

        // 28. Biển đảo Phú Quý (Du lịch, Nhiếp ảnh)
        AddPost("22222222-2222-2222-2222-222222222128", "11111111-1111-1111-1111-111111111104",
            "🌊 Lặn biển ngắm rạn san hô nguyên sơ tại hòn Tranh - đảo Phú Quý sáng nay.\n\nNước biển trong vắt như gương, thấy rõ từng đàn cá tung tăng bơi lượn quanh những nhánh san hô rực rỡ dưới đáy đại dương. Gió biển mặn mòi, lòng người hiền hòa mến khách khiến hòn đảo tiền tiêu này trở thành viên ngọc quý giữa biển đông!",
            new List<string> { "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=1000&auto=format&fit=crop&q=80" },
            180, 15, 850, now.AddHours(-28),
            ("00000000-0000-0000-0000-000000000111", 0.99),
            ("00000000-0000-0000-0000-000000000114", 0.95));

        return (posts, postInterests);
    }

    private static List<Comment> GetSeedComments()
    {
        var now = DateTime.UtcNow;
        var comments = new List<Comment>();

        void AddComment(string id, string postId, string authorId, string content, string? parentId = null) =>
            comments.Add(new Comment
            {
                Id = Guid.Parse(id),
                PostId = Guid.Parse(postId),
                AuthorId = Guid.Parse(authorId),
                Content = content,
                ParentCommentId = parentId != null ? Guid.Parse(parentId) : null,
                CreatedAtUtc = now.AddHours(-3),
                UpdatedAtUtc = now.AddHours(-3)
            });

        // Bình luận bài AI Agent (Post 1)
        AddComment("33333333-3333-3333-3333-333333333101", "22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111107", "Bài viết rất thực tế anh Nam ơi! Đội tech bên startup mình cũng vừa tích hợp AI PR Review, giảm được 50% thời gian merge code.");
        AddComment("33333333-3333-3333-3333-333333333102", "22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111101", "Chuẩn luôn Minh! Quan trọng nhất vẫn là human-in-the-loop để kiểm duyệt logic kinh doanh cốt lõi.", "33333333-3333-3333-3333-333333333101");
        AddComment("33333333-3333-3333-3333-333333333103", "22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111102", "Giao diện và dashboard quản lý AI agent nhìn trực quan và hiện đại quá!");

        // Bình luận bài Dark Mode (Post 2)
        AddComment("33333333-3333-3333-3333-333333333104", "22222222-2222-2222-2222-222222222102", "11111111-1111-1111-1111-111111111101", "Đồng ý 100% với Linh! Nền đen hoàn toàn `#000000` trên màn hình OLED dễ gây mỏi mắt vì độ tương phản gắt.");
        AddComment("33333333-3333-3333-3333-333333333105", "22222222-2222-2222-2222-222222222102", "11111111-1111-1111-1111-111111111103", "Là một gamer, mình vote 1000 phiếu cho Dark Mode tone xanh đen huyền bí 🎮");

        // Bình luận bài Hà Giang (Post 4)
        AddComment("33333333-3333-3333-3333-333333333106", "22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111109", "Màu ảnh và ánh sáng buổi sớm trong veo đẹp mê hồn Chi ơi! Chụp bằng ống tiêu cự nào vậy bạn?");
        AddComment("33333333-3333-3333-3333-333333333107", "22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111104", "Mình chụp bằng lens 24-70mm f/2.8 đó Long, bắt góc siêu rộng trên đỉnh đèo!", "33333333-3333-3333-3333-333333333106");
        AddComment("33333333-3333-3333-3333-333333333108", "22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111106", "Nhìn ảnh mà thèm đi Hà Giang quá, mùa này ăn cháo ấu tẩu nóng thì tuyệt đỉnh.");

        // Bình luận bài Phở Bò (Post 6)
        AddComment("33333333-3333-3333-3333-333333333109", "22222222-2222-2222-2222-222222222106", "11111111-1111-1111-1111-111111111105", "Bát phở giàu đạm chất lượng quá chị Hương! Ăn sau buổi chạy sáng thì hồi phục cơ bắp cực đỉnh.");
        AddComment("33333333-3333-3333-3333-333333333110", "22222222-2222-2222-2222-222222222106", "11111111-1111-1111-1111-111111111104", "Nhìn nước dùng trong veo nổi váng mỡ gừng là biết chuẩn vị phở phố cổ rồi chị ơi 🤤");

        return comments;
    }

    private static List<PostLike> GetSeedLikes()
    {
        var now = DateTime.UtcNow;
        var likes = new List<PostLike>();

        void Like(string postId, string userId) =>
            likes.Add(new PostLike
            {
                PostId = Guid.Parse(postId),
                UserId = Guid.Parse(userId),
                CreatedAtUtc = now.AddHours(-2)
            });

        Like("22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111102");
        Like("22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111103");
        Like("22222222-2222-2222-2222-222222222101", "11111111-1111-1111-1111-111111111107");
        Like("22222222-2222-2222-2222-222222222102", "11111111-1111-1111-1111-111111111101");
        Like("22222222-2222-2222-2222-222222222102", "11111111-1111-1111-1111-111111111109");
        Like("22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111101");
        Like("22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111106");
        Like("22222222-2222-2222-2222-222222222104", "11111111-1111-1111-1111-111111111109");
        Like("22222222-2222-2222-2222-222222222106", "11111111-1111-1111-1111-111111111104");
        Like("22222222-2222-2222-2222-222222222106", "11111111-1111-1111-1111-111111111105");

        return likes;
    }

    private static List<UserInteraction> GetSeedInteractions()
    {
        var now = DateTime.UtcNow;
        var list = new List<UserInteraction>();

        void Interact(string userId, string postId, InteractionType type, double val) =>
            list.Add(new UserInteraction
            {
                Id = Guid.NewGuid(),
                UserId = Guid.Parse(userId),
                PostId = Guid.Parse(postId),
                InteractionType = type,
                Value = val,
                CreatedAtUtc = now.AddHours(-1),
                UpdatedAtUtc = now.AddHours(-1)
            });

        Interact("11111111-1111-1111-1111-111111111101", "22222222-2222-2222-2222-222222222102", InteractionType.Like, 3.0);
        Interact("11111111-1111-1111-1111-111111111101", "22222222-2222-2222-2222-222222222104", InteractionType.View, 1.0);
        Interact("11111111-1111-1111-1111-111111111102", "22222222-2222-2222-2222-222222222101", InteractionType.Comment, 5.0);
        Interact("11111111-1111-1111-1111-111111111103", "22222222-2222-2222-2222-222222222115", InteractionType.Like, 3.0);
        Interact("11111111-1111-1111-1111-111111111104", "22222222-2222-2222-2222-222222222106", InteractionType.Like, 3.0);

        return list;
    }
}
