using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetLocalFillerContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // One-time cleanup: when no content backfill had run, opening a topic in any skill cached
            // the deterministic Local stand-in's repetitive filler ("...the word X helps us explain our
            // ideas clearly", etc.) into PostgreSQL, and it stuck (Status=Filled never refreshes). The
            // request path now lazy-fills via Gemini, so reset every filler row to Pending (Status=0) by
            // matching its tell-tale template text - leaving genuine curated/LLM content untouched. Each
            // reset row regenerates with real content on next open. (Vocabulary is handled separately by
            // bumping VocabularyTopic.CurrentContentVersion, which already forces a refresh.)

            // Reading - LocalReadingContentGenerator intro line.
            migrationBuilder.Sql(
                "UPDATE \"ReadingPassages\" SET \"Status\" = 0 " +
                "WHERE \"Body\" LIKE '%notice how each useful word is used in a sentence%';");

            // Listening - drop the stale synthesized audio (keyed by ExerciseId, so it would otherwise be
            // served against the new transcript) before resetting the filler exercises.
            migrationBuilder.Sql(
                "DELETE FROM \"ListeningAudioClips\" WHERE \"ExerciseId\" IN (" +
                "SELECT \"Id\" FROM \"ListeningExercises\" " +
                "WHERE \"Transcript\" LIKE '%Hello, and welcome. Today I want to talk about%');");
            migrationBuilder.Sql(
                "UPDATE \"ListeningExercises\" SET \"Status\" = 0 " +
                "WHERE \"Transcript\" LIKE '%Hello, and welcome. Today I want to talk about%';");

            // Grammar - LocalGrammarContentGenerator context-intro line.
            migrationBuilder.Sql(
                "UPDATE \"GrammarLessons\" SET \"Status\" = 0 " +
                "WHERE \"ContextIntro\" LIKE '%first study the pattern, then try the exercises below%';");

            // Books - LocalBookContentGenerator closing line of every section body.
            migrationBuilder.Sql(
                "UPDATE \"BookSections\" SET \"Status\" = 0 " +
                "WHERE \"Body\" LIKE '%answer the ten questions to show that you understood this section%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data cleanup: the discarded filler cannot (and should not) be restored.
        }
    }
}
