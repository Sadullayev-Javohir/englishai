using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PremiumWaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Contact = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContactKind = table.Column<int>(type: "integer", nullable: false),
                    InterestedPlan = table.Column<int>(type: "integer", nullable: true),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumWaitlistEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PremiumWaitlistEntries_Contact",
                table: "PremiumWaitlistEntries",
                column: "Contact",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PremiumWaitlistEntries_CreatedAt",
                table: "PremiumWaitlistEntries",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PremiumWaitlistEntries");
        }
    }
}
