using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnergySpendLedgerAndUnitRefill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EnergyRefilledAt",
                table: "LearnerPoints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "date_trunc('hour', now() at time zone 'utc') at time zone 'utc'");

            migrationBuilder.CreateTable(
                name: "EnergySpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LearnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SpentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnergySpends", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnergySpends_LearnerId_Action_ReferenceId",
                table: "EnergySpends",
                columns: new[] { "LearnerId", "Action", "ReferenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnergySpends");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EnergyRefilledAt",
                table: "LearnerPoints",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "date_trunc('hour', now() at time zone 'utc') at time zone 'utc'",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now() at time zone 'utc'");
        }
    }
}
