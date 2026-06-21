using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeakingPracticeWords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpeakingPracticeWords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedWord = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastAccuracyScore = table.Column<double>(type: "double precision", nullable: false),
                    LastErrorType = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    BestPracticeScore = table.Column<double>(type: "double precision", nullable: true),
                    FirstFailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastFailedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MasteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeakingPracticeWords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingPracticeWords_LearnerId_MasteredAt_LastFailedAt",
                table: "SpeakingPracticeWords",
                columns: new[] { "LearnerId", "MasteredAt", "LastFailedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SpeakingPracticeWords_LearnerId_NormalizedWord",
                table: "SpeakingPracticeWords",
                columns: new[] { "LearnerId", "NormalizedWord" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeakingPracticeWords");

        }
    }
}
