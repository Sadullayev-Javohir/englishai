using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardAndPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscountRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoinsSpent = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountRedemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LearnerPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LifetimeXp = table.Column<long>(type: "bigint", nullable: false),
                    SpendableCoins = table.Column<long>(type: "bigint", nullable: false),
                    HighestStreakMilestoneReached = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearnerPoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRedemptions_Code",
                table: "DiscountRedemptions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscountRedemptions_LearnerId",
                table: "DiscountRedemptions",
                column: "LearnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LearnerPoints_LearnerId",
                table: "LearnerPoints",
                column: "LearnerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountRedemptions");

            migrationBuilder.DropTable(
                name: "LearnerPoints");
        }
    }
}
