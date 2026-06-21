using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListeningSegments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListeningSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StartMs = table.Column<int>(type: "integer", nullable: false),
                    EndMs = table.Column<int>(type: "integer", nullable: false),
                    Speaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ListeningExerciseId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListeningSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListeningSegments_ListeningExercises_ListeningExerciseId",
                        column: x => x.ListeningExerciseId,
                        principalTable: "ListeningExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListeningSegments_ListeningExerciseId_Order",
                table: "ListeningSegments",
                columns: new[] { "ListeningExerciseId", "Order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListeningSegments");
        }
    }
}
