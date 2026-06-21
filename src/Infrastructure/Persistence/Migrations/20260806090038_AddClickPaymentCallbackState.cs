using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClickPaymentCallbackState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProviderPaymentId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProviderPrepareId",
                table: "Payments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProviderTransactionId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderPrepareId",
                table: "Payments",
                column: "ProviderPrepareId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderTransactionId",
                table: "Payments",
                column: "ProviderTransactionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderPrepareId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderTransactionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderPaymentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderPrepareId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ProviderTransactionId",
                table: "Payments");
        }
    }
}
