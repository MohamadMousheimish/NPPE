using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPPE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrenchExamContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExplanationForCorrectFr",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExplanationForIncorrectFr",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextFr",
                table: "Questions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextFr",
                table: "AnswerOptions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExplanationForCorrectFr",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "ExplanationForIncorrectFr",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TextFr",
                table: "Questions");

            migrationBuilder.DropColumn(
                name: "TextFr",
                table: "AnswerOptions");
        }
    }
}
