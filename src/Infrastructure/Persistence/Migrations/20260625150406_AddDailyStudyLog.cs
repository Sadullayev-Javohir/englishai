using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyStudyLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyStudyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    SpeakingSeconds = table.Column<int>(type: "integer", nullable: false),
                    ListeningSeconds = table.Column<int>(type: "integer", nullable: false),
                    ReadingSeconds = table.Column<int>(type: "integer", nullable: false),
                    WritingSeconds = table.Column<int>(type: "integer", nullable: false),
                    GrammarSeconds = table.Column<int>(type: "integer", nullable: false),
                    VocabularySeconds = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyStudyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyStudyRecords_LearnerId_Day",
                table: "DailyStudyRecords",
                columns: new[] { "LearnerId", "Day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyStudyRecords");
        }
    }
}
