using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations;

[DbContext(typeof(EnglishAiDbContext))]
[Migration("20260722233000_AddVocabularyDueLookupIndex")]
public partial class AddVocabularyDueLookupIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS "IX_VocabularyItems_LearnerId_Schedule_NextReviewAt"
            ON "VocabularyItems" ("LearnerId", "Schedule_NextReviewAt")
            WHERE "Schedule_NextReviewAt" IS NOT NULL AND "Schedule_Stage" <> 3;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS \"IX_VocabularyItems_LearnerId_Schedule_NextReviewAt\";");
    }
}
