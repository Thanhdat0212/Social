CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "Email" character varying(256) NOT NULL,
        "NormalizedEmail" character varying(256) NOT NULL,
        "PasswordHash" text NOT NULL,
        "DisplayName" character varying(100) NOT NULL,
        "Bio" character varying(500),
        "AvatarUrl" character varying(500),
        "AvatarPublicId" character varying(256),
        "EmailConfirmed" boolean NOT NULL DEFAULT FALSE,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE TABLE "RefreshTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(256) NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "CreatedByIp" character varying(50),
        "RevokedAtUtc" timestamp with time zone,
        "ReplacedByTokenHash" character varying(256),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE TABLE "VerificationTokens" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "TokenHash" character varying(256) NOT NULL,
        "Purpose" character varying(50) NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "ConsumedAtUtc" timestamp with time zone,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_VerificationTokens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_VerificationTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE INDEX "IX_RefreshTokens_UserId" ON "RefreshTokens" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE UNIQUE INDEX "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    CREATE INDEX "IX_VerificationTokens_UserId_Purpose" ON "VerificationTokens" ("UserId", "Purpose");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923100741_Initial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260923100741_Initial', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923152002_AddGoogleAuth') THEN
    ALTER TABLE "Users" ALTER COLUMN "PasswordHash" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923152002_AddGoogleAuth') THEN
    ALTER TABLE "Users" ADD "GoogleId" character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923152002_AddGoogleAuth') THEN
    CREATE UNIQUE INDEX "IX_Users_GoogleId" ON "Users" ("GoogleId") WHERE "GoogleId" IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260923152002_AddGoogleAuth') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260923152002_AddGoogleAuth', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE TABLE "Interests" (
        "Id" uuid NOT NULL,
        "Name" character varying(100) NOT NULL,
        "Slug" character varying(100) NOT NULL,
        "Description" character varying(300),
        "Icon" character varying(50),
        "IsActive" boolean NOT NULL DEFAULT TRUE,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Interests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE TABLE "UserInterests" (
        "UserId" uuid NOT NULL,
        "InterestId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserInterests" PRIMARY KEY ("UserId", "InterestId"),
        CONSTRAINT "FK_UserInterests_Interests_InterestId" FOREIGN KEY ("InterestId") REFERENCES "Interests" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserInterests_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE TABLE "UserPreferences" (
        "UserId" uuid NOT NULL,
        "InterestId" uuid NOT NULL,
        "Score" double precision NOT NULL DEFAULT 1.0,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("UserId", "InterestId"),
        CONSTRAINT "FK_UserPreferences_Interests_InterestId" FOREIGN KEY ("InterestId") REFERENCES "Interests" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserPreferences_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000101', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Tin tức, phần cứng, tiện ích và xu hướng công nghệ mới nhất', '💻', TRUE, 'Công nghệ', 'technology', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000102', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Phát triển phần mềm, thuật toán, web, mobile và hệ thống', '👨‍💻', TRUE, 'Lập trình', 'programming', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000103', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Machine Learning, Deep Learning, Generative AI và công nghệ tương lai', '🤖', TRUE, 'Trí tuệ nhân tạo (AI)', 'ai', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000104', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Game PC, Console, Mobile, Esports và tin tức làng game', '🎮', TRUE, 'Trò chơi (Gaming)', 'gaming', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000105', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Bóng đá đỉnh cao, tin tức thể thao và giải đấu trong nước quốc tế', '⚽', TRUE, 'Thể thao & Bóng đá', 'sports', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000106', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Giai điệu hot, nghệ sĩ, concert, nhạc cụ và các bản hit mới', '🎵', TRUE, 'Âm nhạc', 'music', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000107', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Bom tấn chiếu rạp, series truyền hình, review phim và điện ảnh', '🎬', TRUE, 'Phim ảnh & Điện ảnh', 'movies', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000108', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'UI/UX, đồ họa, typography, kiến trúc và sáng tạo thị giác', '🎨', TRUE, 'Thiết kế & Nghệ thuật', 'design', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000109', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Tài chính, startup, marketing, kinh tế số và đầu tư', '💼', TRUE, 'Kinh doanh & Khởi nghiệp', 'business', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000110', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Khám phá khoa học, thiên văn vũ trụ, vật lý và tự nhiên', '🔬', TRUE, 'Khoa học & Vũ trụ', 'science', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000111', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Điểm đến hấp dẫn, cẩm nang phượt, phong cảnh và khám phá thế giới', '✈️', TRUE, 'Du lịch & Trải nghiệm', 'travel', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000112', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Món ngon mỗi ngày, công thức nấu ăn ngon và review ẩm thực', '🍜', TRUE, 'Ẩm thực & Nấu ăn', 'food', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000113', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Tập luyện gym, yoga, dinh dưỡng khoa học và phong cách sống khỏe', '🏋️', TRUE, 'Sức khỏe & Fitness', 'fitness', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000114', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Kỹ thuật chụp ảnh, máy ảnh, màu sắc và khoảnh khắc cuộc sống', '📷', TRUE, 'Nhiếp ảnh', 'photography', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000115', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Văn hóa truyện tranh, anime Nhật Bản, cosplay và cộng đồng fan', '⛩️', TRUE, 'Anime & Manga', 'anime', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    INSERT INTO "Interests" ("Id", "CreatedAtUtc", "Description", "Icon", "IsActive", "Name", "Slug", "UpdatedAtUtc")
    VALUES ('00000000-0000-0000-0000-000000000116', TIMESTAMPTZ '2026-01-01T00:00:00Z', 'Review sách, thói quen đọc, phát triển tư duy và văn học', '📚', TRUE, 'Sách & Tri thức', 'books', TIMESTAMPTZ '2026-01-01T00:00:00Z');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE UNIQUE INDEX "IX_Interests_Slug" ON "Interests" ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE INDEX "IX_UserInterests_InterestId" ON "UserInterests" ("InterestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    CREATE INDEX "IX_UserPreferences_InterestId" ON "UserPreferences" ("InterestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925051205_AddInterestsAndUserPreferences') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925051205_AddInterestsAndUserPreferences', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    CREATE TABLE "Posts" (
        "Id" uuid NOT NULL,
        "AuthorId" uuid NOT NULL,
        "Content" character varying(5000) NOT NULL,
        "MediaUrls" text[] NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Posts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Posts_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    CREATE TABLE "PostInterests" (
        "PostId" uuid NOT NULL,
        "InterestId" uuid NOT NULL,
        "Confidence" double precision NOT NULL DEFAULT 1.0,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PostInterests" PRIMARY KEY ("PostId", "InterestId"),
        CONSTRAINT "FK_PostInterests_Interests_InterestId" FOREIGN KEY ("InterestId") REFERENCES "Interests" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PostInterests_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    CREATE INDEX "IX_PostInterests_InterestId" ON "PostInterests" ("InterestId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    CREATE INDEX "IX_Posts_AuthorId" ON "Posts" ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    CREATE INDEX "IX_Posts_CreatedAtUtc" ON "Posts" ("CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260925085414_AddPostsAndPostInterests') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260925085414_AddPostsAndPostInterests', '8.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    ALTER TABLE "Posts" ADD "CommentCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    ALTER TABLE "Posts" ADD "LikeCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    ALTER TABLE "Posts" ADD "ViewCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE TABLE "Comments" (
        "Id" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "AuthorId" uuid NOT NULL,
        "ParentCommentId" uuid,
        "Content" character varying(2000) NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_Comments_Comments_ParentCommentId" FOREIGN KEY ("ParentCommentId") REFERENCES "Comments" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Comments_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_Comments_Users_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE TABLE "PostLikes" (
        "UserId" uuid NOT NULL,
        "PostId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PostLikes" PRIMARY KEY ("UserId", "PostId"),
        CONSTRAINT "FK_PostLikes_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_PostLikes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE TABLE "UserFollows" (
        "FollowerId" uuid NOT NULL,
        "FollowingId" uuid NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserFollows" PRIMARY KEY ("FollowerId", "FollowingId"),
        CONSTRAINT "FK_UserFollows_Users_FollowerId" FOREIGN KEY ("FollowerId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserFollows_Users_FollowingId" FOREIGN KEY ("FollowingId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE TABLE "UserInteractions" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "PostId" uuid,
        "InteractionType" integer NOT NULL,
        "Value" double precision NOT NULL DEFAULT 1.0,
        "Metadata" character varying(1000),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserInteractions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_UserInteractions_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_UserInteractions_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_Comments_AuthorId" ON "Comments" ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_Comments_CreatedAtUtc" ON "Comments" ("CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_Comments_ParentCommentId" ON "Comments" ("ParentCommentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_Comments_PostId" ON "Comments" ("PostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_PostLikes_CreatedAtUtc" ON "PostLikes" ("CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_PostLikes_PostId" ON "PostLikes" ("PostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserFollows_CreatedAtUtc" ON "UserFollows" ("CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserFollows_FollowingId" ON "UserFollows" ("FollowingId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserInteractions_CreatedAtUtc" ON "UserInteractions" ("CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserInteractions_InteractionType" ON "UserInteractions" ("InteractionType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserInteractions_PostId" ON "UserInteractions" ("PostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserInteractions_UserId" ON "UserInteractions" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    CREATE INDEX "IX_UserInteractions_UserId_InteractionType_CreatedAtUtc" ON "UserInteractions" ("UserId", "InteractionType", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260928053555_AddInteractionsAndSocialFeatures') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260928053555_AddInteractionsAndSocialFeatures', '8.0.11');
    END IF;
END $EF$;
COMMIT;

