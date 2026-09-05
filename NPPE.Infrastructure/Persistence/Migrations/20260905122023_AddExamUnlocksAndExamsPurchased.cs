using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NPPE.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExamUnlocksAndExamsPurchased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExamsPurchased",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ExamUnlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttemptsAllowed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamUnlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamUnlocks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamUnlocks_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExamUnlocks_ExamId",
                table: "ExamUnlocks",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamUnlocks_UserId_ExamId",
                table: "ExamUnlocks",
                columns: new[] { "UserId", "ExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamUnlocks");

            migrationBuilder.DropColumn(
                name: "ExamsPurchased",
                table: "Payments");
        }
    }
}
