using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProTrial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProTrialExpiresAt",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"UserAccounts\" SET \"ProTrialExpiresAt\" = CURRENT_TIMESTAMP + INTERVAL '30 days'");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ProTrialExpiresAt",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProTrialExpiresAt",
                table: "UserAccounts");
        }
    }
}
