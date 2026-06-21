using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class AddDurableNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Notifications", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LearnerId_Code_CreatedAt",
                table: "Notifications",
                columns: new[] { "LearnerId", "Code", "CreatedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LearnerId_CreatedAt",
                table: "Notifications",
                columns: new[] { "LearnerId", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Notifications");
        }
    }
}
