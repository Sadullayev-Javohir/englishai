using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Infrastructure.Persistence.Migrations;

/// <summary>
/// Advances only ordinary, unfinished final checkpoints. Operator-forced due dates,
/// recovery reviews and mastered-word maintenance dates are intentionally untouched.
/// The persisted stage ordinal remains 2; no learning history is discarded.
/// Hour intervals match DateTimeOffset.AddDays even when the database session uses a DST zone.
/// </summary>
[DbContext(typeof(EnglishAiDbContext))]
[Migration("20260910120000_UseDay21VocabularyReviews")]
public sealed class UseDay21VocabularyReviews : Migration
{
    public const string MigrationSql = """
        UPDATE "VocabularyItems"
        SET "Schedule_NextReviewAt" = "Schedule_LearnedAt" + INTERVAL '504 hours'
        WHERE "Schedule_Stage" = 2
          AND "Schedule_NextReviewAt" = "Schedule_LearnedAt" + INTERVAL '720 hours';
        """;

    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql(MigrationSql);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "This review-date migration is forward-only. Restore the pre-release database backup " +
            "with the matching application release; adding nine days would corrupt intervening reviews.");
}
