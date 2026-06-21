using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_LearnerId_Type",
                table: "ProductEvents",
                columns: new[] { "LearnerId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_LearnerId_Type_Source",
                table: "ProductEvents",
                columns: new[] { "LearnerId", "Type", "Source" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductEvents_OccurredAt",
                table: "ProductEvents",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductEvents");
        }
    }
}
