-- Migration bổ sung bảng PostEmbeddings cho Hệ thống Semantic Vector Recommendation
START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'public' AND tablename = 'PostEmbeddings') THEN
        CREATE TABLE "PostEmbeddings" (
            "PostId" uuid NOT NULL,
            "Values" real[] NOT NULL,
            "Model" character varying(100) NOT NULL,
            "CreatedAtUtc" timestamp with time zone NOT NULL,
            "UpdatedAtUtc" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_PostEmbeddings" PRIMARY KEY ("PostId"),
            CONSTRAINT "FK_PostEmbeddings_Posts_PostId" FOREIGN KEY ("PostId") REFERENCES "Posts" ("Id") ON DELETE CASCADE
        );
        CREATE INDEX "IX_PostEmbeddings_CreatedAtUtc" ON "PostEmbeddings" ("CreatedAtUtc");
    END IF;
END $EF$;

COMMIT;
