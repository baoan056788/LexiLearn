using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyNotes",
                columns: table => new
                {
                    StudyNoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SetId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RelatedTerm = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPinned = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyNotes", x => x.StudyNoteId);
                    table.ForeignKey(
                        name: "FK_StudyNotes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyNotes_VocabularySets_SetId",
                        column: x => x.SetId,
                        principalTable: "VocabularySets",
                        principalColumn: "SetId",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_SetId",
                table: "StudyNotes",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_UserId_IsPinned_UpdatedAt",
                table: "StudyNotes",
                columns: new[] { "UserId", "IsPinned", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_UserId_UpdatedAt",
                table: "StudyNotes",
                columns: new[] { "UserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyNotes");
        }
    }
}
