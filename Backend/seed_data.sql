-- ==============================================================================
-- SOCIAL PLATFORM - FULL SEED DATA FOR TESTING & LOCAL / PRODUCTION VERIFICATION
-- Mật khẩu mặc định cho tất cả tài khoản test: Password123@
-- ==============================================================================
START TRANSACTION;

-- 1. SEED TÀI KHOẢN NGƯỜI DÙNG (USERS)
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', 'nam.tech@social.com', 'NAM.TECH@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Nguyễn Hoàng Nam', 'Senior Software Engineer & AI Researcher. Đam mê chia sẻ kiến thức về Microservices, AI Agents & Cloud Native. 🚀', 'https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', 'linh.design@social.com', 'LINH.DESIGN@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Lê Ngọc Linh', 'Product Designer & Visual Artist 🎨. Yêu thích giao diện tối giản, trải nghiệm người dùng mượt mà & Typography.', 'https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', 'tuan.gamer@social.com', 'TUAN.GAMER@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Trần Quốc Tuấn', 'Esports caster & hardcore gamer. Chuyên review các tựa game AAA và phần cứng PC gaming hàng đầu. 🎮🔥', 'https://images.unsplash.com/photo-1570295999919-56ceb5ecca61?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', 'mai.travel@social.com', 'MAI.TRAVEL@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Hoàng Mai Chi', 'Travel Creator & Travel Blogger ✈️. Đi qua 25 tỉnh thành và 8 quốc gia. Chụp ảnh, thưởng thức ẩm thực đường phố và ghi lại khoảnh khắc.', 'https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', 'phong.fitness@social.com', 'PHONG.FITNESS@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Đặng Hải Phong', 'Certified Fitness Coach & Nutritionist 🏋️‍♂️. Lan tỏa lối sống năng động, kỷ luật rèn luyện và chế độ ăn uống khoa học.', 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', 'huong.foodie@social.com', 'HUONG.FOODIE@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Phạm Thu Hương', 'Home Chef & Food Reviewer 🍜. Yêu ẩm thực Việt Nam truyền thống và sáng tạo những công thức nấu ăn ngon dễ làm mỗi ngày.', 'https://images.unsplash.com/photo-1517841905240-472988babdf9?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', 'minh.startup@social.com', 'MINH.STARTUP@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Vũ Tuấn Minh', 'Founder & Tech Angel Investor 💼. Chia sẻ bài học về Product Management, Growth Hacking và xây dựng đội ngũ công nghệ tinh gọn.', 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;
INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "Bio", "AvatarUrl", "AvatarPublicId", "EmailConfirmed", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', 'lan.reader@social.com', 'LAN.READER@SOCIAL.COM', 'AQAAAAEAAYagAAAAEPdq8PSDH11CLwIgcC7XpeeJqZx/u6YfmMsvYooeSM6nFzTkmzoXExtUZNM8bGj5pw==', 'Trần Ngọc Lan', 'Book Reviewer & Podcaster 📚🎙️. Một cuốn sách mỗi tuần. Cùng nhau khám phá tri thức, tâm lý học và nghệ thuật sống an yên.', 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=400&auto=format&fit=crop&q=80', 'seed_avatar_11111111', TRUE, NOW() - INTERVAL '10 days', NOW())
ON CONFLICT ("Id") DO UPDATE SET "DisplayName" = EXCLUDED."DisplayName", "Bio" = EXCLUDED."Bio", "AvatarUrl" = EXCLUDED."AvatarUrl", "PasswordHash" = EXCLUDED."PasswordHash", "EmailConfirmed" = TRUE;

-- 2. SEED SỞ THÍCH & TRỌNG SỐ THUẬT TOÁN CHO USER (USER INTERESTS & PREFERENCES)
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000101', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000101', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000102', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000102', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000103', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000103', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000110', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '00000000-0000-0000-0000-000000000110', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000108', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000108', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000114', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000114', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000101', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000101', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000107', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '00000000-0000-0000-0000-000000000107', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000104', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000104', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000115', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000115', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000101', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000101', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000106', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '00000000-0000-0000-0000-000000000106', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000111', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000111', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000112', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000112', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000114', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000114', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000116', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '00000000-0000-0000-0000-000000000116', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000113', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000113', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000105', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000105', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000112', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000112', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000110', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '00000000-0000-0000-0000-000000000110', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000112', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000112', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000111', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000111', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000114', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000114', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000108', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '00000000-0000-0000-0000-000000000108', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000109', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000109', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000101', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000101', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000103', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000103', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000116', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '00000000-0000-0000-0000-000000000116', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000116', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000116', 3.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000107', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000107', 3.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000106', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000106', 2.5, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";
INSERT INTO "UserInterests" ("UserId", "InterestId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000110', NOW() - INTERVAL '9 days')
ON CONFLICT ("UserId", "InterestId") DO NOTHING;
INSERT INTO "UserPreferences" ("UserId", "InterestId", "Score", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '00000000-0000-0000-0000-000000000110', 2.0, NOW() - INTERVAL '9 days', NOW())
ON CONFLICT ("UserId", "InterestId") DO UPDATE SET "Score" = EXCLUDED."Score";

-- 3. SEED QUAN HỆ THEO DÕI (USER FOLLOWS)
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '11111111-1111-1111-1111-111111111102', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '11111111-1111-1111-1111-111111111103', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '11111111-1111-1111-1111-111111111107', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '11111111-1111-1111-1111-111111111108', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '11111111-1111-1111-1111-111111111101', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '11111111-1111-1111-1111-111111111104', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '11111111-1111-1111-1111-111111111106', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '11111111-1111-1111-1111-111111111101', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '11111111-1111-1111-1111-111111111105', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '11111111-1111-1111-1111-111111111102', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '11111111-1111-1111-1111-111111111105', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '11111111-1111-1111-1111-111111111106', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '11111111-1111-1111-1111-111111111108', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '11111111-1111-1111-1111-111111111101', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '11111111-1111-1111-1111-111111111104', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '11111111-1111-1111-1111-111111111106', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '11111111-1111-1111-1111-111111111102', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '11111111-1111-1111-1111-111111111104', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111106', '11111111-1111-1111-1111-111111111105', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '11111111-1111-1111-1111-111111111101', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '11111111-1111-1111-1111-111111111102', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111107', '11111111-1111-1111-1111-111111111108', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '11111111-1111-1111-1111-111111111101', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '11111111-1111-1111-1111-111111111104', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;
INSERT INTO "UserFollows" ("FollowerId", "FollowingId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111108', '11111111-1111-1111-1111-111111111107', NOW() - INTERVAL '8 days')
ON CONFLICT ("FollowerId", "FollowingId") DO NOTHING;

-- 4. SEED BÀI VIẾT (POSTS) & GẮN THẺ THUẬT TOÁN (POST INTERESTS)
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222101', '11111111-1111-1111-1111-111111111101', '🚀 Tổng kết 6 tháng ứng dụng mô hình AI Agent tự hành vào quy trình phát triển phần mềm:

1. Tốc độ code review tăng 45% nhờ tự động phát hiện logic flaws.
2. Tự động sinh integration test cases bao phủ các edge cases khó.
3. Documentation luôn được cập nhật đồng bộ với schema DB.

AI không thay thế lập trình viên, nhưng lập trình viên biết khai thác AI hiệu quả sẽ bứt phá rất xa! Anh em nghĩ sao về xu hướng này?', ARRAY['https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?w=1000&auto=format&fit=crop&q=80']::text[], 1, 68, 6, 420, NOW() - INTERVAL '12 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222101', '00000000-0000-0000-0000-000000000103', 0.98, NOW() - INTERVAL '12 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222101', '00000000-0000-0000-0000-000000000102', 0.95, NOW() - INTERVAL '12 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222101', '00000000-0000-0000-0000-000000000101', 0.90, NOW() - INTERVAL '12 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222102', '11111111-1111-1111-1111-111111111102', '✨ Dark Mode không chỉ đơn giản là đổi màu nền sang đen `#000000`!

Trong thiết kế UI hiện đại:
- Hãy sử dụng dải màu Dark Gray (như `#0a0a0f`, `#12121a`) để mắt không bị gắt.
- Tạo chiều sâu cho Cards bằng viền Glassmorphism tinh tế `rgba(255, 255, 255, 0.08)` và bóng mờ đa tầng.
- Giữ độ tương phản văn bản tối thiểu 4.5:1 để đảm bảo tính tiếp cận (Accessibility).

Mọi người thích giao diện Dark Mode huyền bí hay Light Mode trong sáng hơn?', ARRAY['https://images.unsplash.com/photo-1507238691740-187a5b1d37b8?w=1000&auto=format&fit=crop&q=80']::text[], 1, 54, 4, 380, NOW() - INTERVAL '11 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222102', '00000000-0000-0000-0000-000000000108', 0.97, NOW() - INTERVAL '11 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222102', '00000000-0000-0000-0000-000000000101', 0.85, NOW() - INTERVAL '11 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222103', '11111111-1111-1111-1111-111111111103', '🎮 Vừa phá đảo Black Myth: Wukong ở chế độ New Game+! Phải công nhận khâu thiết kế boss fight và hiệu ứng đồ họa Unreal Engine 5 đỉnh cao thực sự. 

Cảnh quan núi non hùng vĩ và âm nhạc mang đậm chất thần thoại phương Đông làm mình nổi da gà nhiều đoạn. Anh em đã ai mở khóa hết toàn bộ vũ khí và pháp bảo ẩn chưa?', ARRAY['https://images.unsplash.com/photo-1542751371-adc38448a05e?w=1000&auto=format&fit=crop&q=80']::text[], 1, 112, 8, 590, NOW() - INTERVAL '10 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222103', '00000000-0000-0000-0000-000000000104', 0.99, NOW() - INTERVAL '10 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222103', '00000000-0000-0000-0000-000000000106', 0.75, NOW() - INTERVAL '10 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222104', '11111111-1111-1111-1111-111111111104', '⛰️ Bình minh trên đỉnh Mã Pí Lèng - Hà Giang sáng nay đẹp như một bức tranh thuỷ mặc!

Không khí se lạnh 14°C, sương mù vờn quanh các ngọn núi đá vôi tai mèo và dòng sông Nho Quế xanh ngọc bích uốn lượn dưới chân đèo. Cảm giác đứng giữa đất trời bao la thực sự gột rửa mọi áp lực công việc. 

Mỗi người trẻ nhất định nên đi Hà Giang ít nhất một lần trong đời!', ARRAY['https://images.unsplash.com/photo-1528127269322-539801943592?w=1000&auto=format&fit=crop&q=80', 'https://images.unsplash.com/photo-1506744038136-46273834b3fb?w=1000&auto=format&fit=crop&q=80']::text[], 1, 145, 9, 670, NOW() - INTERVAL '9 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222104', '00000000-0000-0000-0000-000000000111', 0.99, NOW() - INTERVAL '9 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222104', '00000000-0000-0000-0000-000000000114', 0.92, NOW() - INTERVAL '9 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222105', '11111111-1111-1111-1111-111111111105', '💪 3 sai lầm phổ biến nhất khiến bạn tập Gym mãi không tăng cơ giảm mỡ:

1. Bỏ qua Progressive Overload: Mỗi tuần không tăng mức tạ hoặc số reps.
2. Ăn thiếu Protein: Cơ bắp cần tối thiểu 1.6g - 2.0g Protein / kg thể trọng mỗi ngày.
3. Ngủ ít hơn 7 tiếng: 80% quá trình hồi phục và phát triển cơ bắp diễn ra khi bạn ngủ sâu.

Kỷ luật chính là chiếc cầu nối giữa mục tiêu và kết quả thực tế. Hãy bắt đầu từ hôm nay!', ARRAY['https://images.unsplash.com/photo-1517838277536-f5f99be501cd?w=1000&auto=format&fit=crop&q=80']::text[], 1, 88, 5, 490, NOW() - INTERVAL '8 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222105', '00000000-0000-0000-0000-000000000113', 0.98, NOW() - INTERVAL '8 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222105', '00000000-0000-0000-0000-000000000105', 0.90, NOW() - INTERVAL '8 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222105', '00000000-0000-0000-0000-000000000110', 0.80, NOW() - INTERVAL '8 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222106', '11111111-1111-1111-1111-111111111106', '🍲 Nồi nước dùng Phở bò gia truyền ninh suốt 12 tiếng cuối cùng cũng hoàn thành!

Bí quyết nằm ở xương ống bò rửa sạch chần qua nước sôi gừng sả, rang hoa hồi, thảo quả, quế cây thật thơm rồi ninh lửa nhỏ liu riu không đậy nắp để nước trong veo ngọt thanh tự nhiên. 

Thời tiết se se lạnh thế này mà làm một tô phở tái lăn nóng hổi nhiều hành thì không còn gì tuyệt bằng!', ARRAY['https://images.unsplash.com/photo-1582878826629-29b7ad1cdc43?w=1000&auto=format&fit=crop&q=80']::text[], 1, 95, 7, 530, NOW() - INTERVAL '7 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222106', '00000000-0000-0000-0000-000000000112', 0.99, NOW() - INTERVAL '7 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222106', '00000000-0000-0000-0000-000000000114', 0.70, NOW() - INTERVAL '7 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222107', '11111111-1111-1111-1111-111111111107', '💡 Bài học đắt giá khi xây dựng sản phẩm từ con số 0:

Đừng mất 6 tháng trong phòng kín để hoàn thiện mọi tính năng mà người dùng không cần. Hãy ship bản MVP (Minimum Viable Product) thật nhanh trong vòng 3-4 tuần để kiểm chứng giả thuyết thị trường.

Lắng nghe feedback của 100 người dùng đầu tiên quan trọng hơn gấp 10 lần việc ngồi suy đoán. Tốc độ thực thi và khả năng thích ứng chính là lợi thế cạnh tranh lớn nhất của Startup.', ARRAY['https://images.unsplash.com/photo-1552664730-d307ca884978?w=1000&auto=format&fit=crop&q=80']::text[], 1, 130, 8, 610, NOW() - INTERVAL '6 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222107', '00000000-0000-0000-0000-000000000109', 0.98, NOW() - INTERVAL '6 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222107', '00000000-0000-0000-0000-000000000101', 0.88, NOW() - INTERVAL '6 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222107', '00000000-0000-0000-0000-000000000116', 0.75, NOW() - INTERVAL '6 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222108', '11111111-1111-1111-1111-111111111108', '📖 Trích đoạn tâm đắc từ cuốn sách ''Deep Work'' của Cal Newport:

''Khả năng tập trung sâu vào một công việc phức tạp mà không bị phân tâm chính là siêu năng lực của thế kỷ 21.''

Trong một thế giới đầy ắp thông báo điện thoại, mạng xã hội và sự xao nhãng liên tục, người biết dành ra 2-3 tiếng mỗi ngày trong trạng thái tập trung tuyệt đối sẽ tạo ra những giá trị đột phá. Bạn thường quản lý thời gian làm việc sâu như thế nào?', ARRAY['https://images.unsplash.com/photo-1497633762265-9d179a990aa6?w=1000&auto=format&fit=crop&q=80']::text[], 1, 77, 4, 410, NOW() - INTERVAL '5 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222108', '00000000-0000-0000-0000-000000000116', 0.98, NOW() - INTERVAL '5 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222108', '00000000-0000-0000-0000-000000000110', 0.85, NOW() - INTERVAL '5 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222108', '00000000-0000-0000-0000-000000000109', 0.70, NOW() - INTERVAL '5 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222109', '11111111-1111-1111-1111-111111111101', '⚡ So sánh hiệu năng thực tế giữa ASP.NET Core (.NET 8/9) và Go khi xử lý 100,000 req/s:

- ASP.NET Core với Minimal APIs + Native AOT cho throughput cực kỳ ấn tượng, tối ưu hoá bộ nhớ rất tốt.
- Go vẫn giữ thế mạnh về startup time tức thì và goroutine siêu nhẹ.

Nhìn chung, cả hai đều là những sự lựa chọn tuyệt vời cho kiến trúc Microservices và hệ thống High-concurrency hiện đại.', ARRAY['https://images.unsplash.com/photo-1555066931-4365d14bab8c?w=1000&auto=format&fit=crop&q=80']::text[], 1, 62, 5, 390, NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222109', '00000000-0000-0000-0000-000000000102', 0.99, NOW() - INTERVAL '4 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222109', '00000000-0000-0000-0000-000000000101', 0.95, NOW() - INTERVAL '4 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222110', '11111111-1111-1111-1111-111111111103', '🔥 Trailer mới nhất của anime Solo Leveling Season 2 đỉnh thực sự anh em ơi! Hiệu ứng bóng tối của Sung Jin-woo và âm nhạc từ Hiroyuki Sawano luôn là sự kết hợp hoàn hảo. Mùa này dự kiến sẽ bùng nổ mạng xã hội đầu năm tới!', ARRAY['https://images.unsplash.com/photo-1578632767115-351597cf2477?w=1000&auto=format&fit=crop&q=80']::text[], 1, 98, 6, 480, NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222110', '00000000-0000-0000-0000-000000000115', 0.99, NOW() - INTERVAL '3 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222110', '00000000-0000-0000-0000-000000000104', 0.80, NOW() - INTERVAL '3 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222110', '00000000-0000-0000-0000-000000000106', 0.70, NOW() - INTERVAL '3 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222111', '11111111-1111-1111-1111-111111111104', '☕ Một buổi chiều bình yên ngắm hoàng hôn tại quán cà phê ven đồi Đà Lạt. Tiếng thông reo trong gió, một tách cappuccino nóng và cuốn sách yêu thích... Đôi khi hạnh phúc chỉ đơn giản là được sống chậm lại một chút.', ARRAY['https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=1000&auto=format&fit=crop&q=80', 'https://images.unsplash.com/photo-1447752875215-b2761acb3c5d?w=1000&auto=format&fit=crop&q=80']::text[], 1, 115, 7, 520, NOW() - INTERVAL '2 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222111', '00000000-0000-0000-0000-000000000111', 0.96, NOW() - INTERVAL '2 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222111', '00000000-0000-0000-0000-000000000114', 0.90, NOW() - INTERVAL '2 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222111', '00000000-0000-0000-0000-000000000112', 0.80, NOW() - INTERVAL '2 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "Posts" ("Id", "AuthorId", "Content", "MediaUrls", "Status", "LikeCount", "CommentCount", "ViewCount", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222112', '11111111-1111-1111-1111-111111111105', '🏃‍♂️ Hoàn thành cự ly Half Marathon 21km sáng nay với thành tích Personal Best 1h48m! 

Cảm giác khi vượt qua km thứ 18 khi đôi chân bắt đầu mỏi nhừ và tâm trí muốn bỏ cuộc chính là lúc sức mạnh tinh thần được thử thách lớn nhất. Chúc mọi người cuối tuần tràn đầy năng lượng tích cực!', ARRAY['https://images.unsplash.com/photo-1452626038306-9aae5e071dd3?w=1000&auto=format&fit=crop&q=80']::text[], 1, 84, 6, 430, NOW() - INTERVAL '1 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content", "MediaUrls" = EXCLUDED."MediaUrls", "LikeCount" = EXCLUDED."LikeCount", "CommentCount" = EXCLUDED."CommentCount", "ViewCount" = EXCLUDED."ViewCount";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222112', '00000000-0000-0000-0000-000000000113', 0.99, NOW() - INTERVAL '1 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";
INSERT INTO "PostInterests" ("PostId", "InterestId", "Confidence", "CreatedAtUtc")
VALUES ('22222222-2222-2222-2222-222222222112', '00000000-0000-0000-0000-000000000105', 0.95, NOW() - INTERVAL '1 hours')
ON CONFLICT ("PostId", "InterestId") DO UPDATE SET "Confidence" = EXCLUDED."Confidence";

-- 5. SEED BÌNH LUẬN & TRẢ LỜI BÌNH LUẬN (COMMENTS)
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333101', '22222222-2222-2222-2222-222222222101', '11111111-1111-1111-1111-111111111107', NULL, 'Bài viết rất thực tế! Bên mình đang ứng dụng AI vào tự động hóa support và năng suất tăng rõ rệt.', NOW() - INTERVAL '20 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333102', '22222222-2222-2222-2222-222222222101', '11111111-1111-1111-1111-111111111102', NULL, 'Phần code review AI có hỗ trợ kiểm tra design token và accessibility không anh Nam ơi?', NOW() - INTERVAL '19 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333103', '22222222-2222-2222-2222-222222222101', '11111111-1111-1111-1111-111111111101', '33333333-3333-3333-3333-333333333102', '@Lê Ngọc Linh Hoàn toàn có thể em nhé! Chỉ cần cung cấp Design System schema vào context của agent.', NOW() - INTERVAL '18 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333104', '22222222-2222-2222-2222-222222222101', '11111111-1111-1111-1111-111111111108', NULL, 'Tương lai công nghệ phát triển nhanh quá, đọc bài anh mở mang thêm nhiều góc nhìn.', NOW() - INTERVAL '17 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333105', '22222222-2222-2222-2222-222222222102', '11111111-1111-1111-1111-111111111101', NULL, 'Chuẩn luôn! Nền đen tuyệt đối #000000 nhìn rất chói và mỏi mắt khi làm việc lâu vào ban đêm.', NOW() - INTERVAL '16 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333106', '22222222-2222-2222-2222-222222222102', '11111111-1111-1111-1111-111111111104', NULL, 'Giao diện web của team mình dùng bảng màu dark mode nhìn sang chảnh thật sự!', NOW() - INTERVAL '15 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333107', '22222222-2222-2222-2222-222222222102', '11111111-1111-1111-1111-111111111107', NULL, 'Chia sẻ rất giá trị! UX tốt là giữ chân người dùng ở lại lâu nhất.', NOW() - INTERVAL '14 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333108', '22222222-2222-2222-2222-222222222103', '11111111-1111-1111-1111-111111111101', NULL, 'Game đỉnh thật sự! Đoạn đánh Nhị Lang Thần trên mây hiệu ứng choáng ngợp luôn.', NOW() - INTERVAL '13 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333109', '22222222-2222-2222-2222-222222222103', '11111111-1111-1111-1111-111111111105', NULL, 'Đang kẹt ở con Hoàng Phong Quái mãi chưa qua đây ông Tuấn ơi haha!', NOW() - INTERVAL '12 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333110', '22222222-2222-2222-2222-222222222103', '11111111-1111-1111-1111-111111111103', '33333333-3333-3333-3333-333333333109', '@Đặng Hải Phong Nhớ dùng Định Phong Châu để khắc chế chiêu bão cát của nó nhé ông!', NOW() - INTERVAL '11 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333111', '22222222-2222-2222-2222-222222222104', '11111111-1111-1111-1111-111111111106', NULL, 'Góc chụp đẹp xuất sắc bạn ơi! Nhìn dòng Nho Quế mà muốn xách balo lên đi ngay.', NOW() - INTERVAL '10 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333112', '22222222-2222-2222-2222-222222222104', '11111111-1111-1111-1111-111111111108', NULL, 'Cảnh đẹp quê hương mình thật tự hào. Mai chụp bằng máy ảnh gì vậy bạn?', NOW() - INTERVAL '9 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333113', '22222222-2222-2222-2222-222222222104', '11111111-1111-1111-1111-111111111104', '33333333-3333-3333-3333-333333333112', '@Trần Ngọc Lan Mình chụp bằng Sony A7IV lens 24-70mm GM II nha bạn ơi!', NOW() - INTERVAL '8 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333114', '22222222-2222-2222-2222-222222222106', '11111111-1111-1111-1111-111111111105', NULL, 'Nhìn hấp dẫn quá chị Hương ơi! Món này ăn sau buổi tập gym bù đạm là chuẩn bài.', NOW() - INTERVAL '7 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333115', '22222222-2222-2222-2222-222222222106', '11111111-1111-1111-1111-111111111102', NULL, 'Cho em xin công thức chi tiết ướp thịt bò với ạ, nhìn thèm xỉu!', NOW() - INTERVAL '6 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333116', '22222222-2222-2222-2222-222222222106', '11111111-1111-1111-1111-111111111106', '33333333-3333-3333-3333-333333333115', '@Lê Ngọc Linh Chị sẽ lên video hướng dẫn chi tiết từng bước vào tối mai nha!', NOW() - INTERVAL '5 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333117', '22222222-2222-2222-2222-222222222107', '11111111-1111-1111-1111-111111111101', NULL, 'Rất đồng tình với anh Minh. Nhiều team đốt tiền và thời gian làm tính năng không ai dùng.', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";
INSERT INTO "Comments" ("Id", "PostId", "AuthorId", "ParentCommentId", "Content", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('33333333-3333-3333-3333-333333333118', '22222222-2222-2222-2222-222222222107', '11111111-1111-1111-1111-111111111108', NULL, 'Bài học giá trị từ cuốn The Lean Startup luôn đúng trong mọi thời đại.', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO UPDATE SET "Content" = EXCLUDED."Content";

-- 6. SEED LƯỢT THÍCH BÀI VIẾT (POST LIKES)
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222101', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222102', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222102', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222103', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222103', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '22222222-2222-2222-2222-222222222103', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222104', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222104', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222104', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '22222222-2222-2222-2222-222222222104', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222105', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222105', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222106', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222106', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222106', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222107', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222107', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222107', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '22222222-2222-2222-2222-222222222107', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222108', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222108', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222108', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '22222222-2222-2222-2222-222222222108', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111105', '22222222-2222-2222-2222-222222222108', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222109', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222110', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222110', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222111', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222111', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222111', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222112', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222112', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222112', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;
INSERT INTO "PostLikes" ("UserId", "PostId", "CreatedAtUtc")
VALUES ('11111111-1111-1111-1111-111111111104', '22222222-2222-2222-2222-222222222112', NOW() - INTERVAL '5 hours')
ON CONFLICT ("UserId", "PostId") DO NOTHING;

-- 7. SEED NHẬT KÝ TƯƠNG TÁC NGƯỜI DÙNG (USER INTERACTIONS FOR AI)
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444101', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222101', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444102', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222101', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444103', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222101', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444104', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222101', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444105', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222101', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444106', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222101', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444107', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222102', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444108', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222102', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444109', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222102', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444110', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222102', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444111', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222102', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444112', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222102', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444113', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222103', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444114', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222103', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444115', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222103', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444116', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222103', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444117', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222103', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444118', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222103', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444119', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222104', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444120', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222104', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444121', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222104', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444122', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222104', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444123', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222104', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444124', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222104', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444125', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222105', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444126', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222105', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444127', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222105', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444128', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222105', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444129', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222105', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444130', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222105', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444131', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222106', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444132', '11111111-1111-1111-1111-111111111101', '22222222-2222-2222-2222-222222222106', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444133', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222106', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444134', '11111111-1111-1111-1111-111111111102', '22222222-2222-2222-2222-222222222106', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444135', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222106', 1, 1.0, '{"dwell_time_seconds": 45}', NOW() - INTERVAL '4 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;
INSERT INTO "UserInteractions" ("Id", "UserId", "PostId", "InteractionType", "Value", "Metadata", "CreatedAtUtc", "UpdatedAtUtc")
VALUES ('44444444-4444-4444-4444-444444444136', '11111111-1111-1111-1111-111111111103', '22222222-2222-2222-2222-222222222106', 2, 3.0, '{"source": "feed"}', NOW() - INTERVAL '3 hours', NOW())
ON CONFLICT ("Id") DO NOTHING;

COMMIT;