using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPPE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentStripeInvoiceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeInvoiceId",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeInvoiceId",
                table: "Payments");
        }
    }
}
