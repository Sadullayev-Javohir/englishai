using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicImageSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicImages",
                table: "TopicImages");

            migrationBuilder.AddColumn<int>(
                name: "Slot",
                table: "TopicImages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicImages",
                table: "TopicImages",
                columns: new[] { "TopicId", "Slot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TopicImages",
                table: "TopicImages");

            migrationBuilder.DropColumn(
                name: "Slot",
                table: "TopicImages");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TopicImages",
                table: "TopicImages",
                column: "TopicId");
        }
    }
}
