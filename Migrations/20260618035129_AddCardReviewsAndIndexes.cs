using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddCardReviewsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tests_UserId",
                table: "Tests");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_UserId",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_Progresses_UserId",
                table: "Progresses");

            migrationBuilder.CreateTable(
                name: "CardReviews",
                columns: table => new
                {
                    CardReviewId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CardId = table.Column<int>(type: "int", nullable: false),
                    RepetitionCount = table.Column<int>(type: "int", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    EaseFactor = table.Column<double>(type: "float", nullable: false),
                    DueAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardReviews", x => x.CardReviewId);
                    table.ForeignKey(
                        name: "FK_CardReviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardReviews_VocabularyCards_CardId",
                        column: x => x.CardId,
                        principalTable: "VocabularyCards",
                        principalColumn: "CardId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularySets_IsPublic_CreatedAt",
                table: "VocabularySets",
                columns: new[] { "IsPublic", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Tests_UserId_CreatedAt",
                table: "Tests",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId_StartedAt",
                table: "StudySessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.Sql("""
                WITH DuplicateProgresses AS (
                    SELECT ProgressId,
                           ROW_NUMBER() OVER (
                               PARTITION BY UserId, SetId
                               ORDER BY UpdatedAt DESC, ProgressId DESC
                           ) AS RowNumber
                    FROM Progresses
                )
                DELETE FROM DuplicateProgresses
                WHERE RowNumber > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_UserId_SetId",
                table: "Progresses",
                columns: new[] { "UserId", "SetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardReviews_CardId",
                table: "CardReviews",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardReviews_UserId_CardId",
                table: "CardReviews",
                columns: new[] { "UserId", "CardId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardReviews_UserId_DueAt",
                table: "CardReviews",
                columns: new[] { "UserId", "DueAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardReviews");

            migrationBuilder.DropIndex(
                name: "IX_VocabularySets_IsPublic_CreatedAt",
                table: "VocabularySets");

            migrationBuilder.DropIndex(
                name: "IX_Tests_UserId_CreatedAt",
                table: "Tests");

            migrationBuilder.DropIndex(
                name: "IX_StudySessions_UserId_StartedAt",
                table: "StudySessions");

            migrationBuilder.DropIndex(
                name: "IX_Progresses_UserId_SetId",
                table: "Progresses");

            migrationBuilder.CreateIndex(
                name: "IX_Tests_UserId",
                table: "Tests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_UserId",
                table: "StudySessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Progresses_UserId",
                table: "Progresses",
                column: "UserId");
        }
    }
}
