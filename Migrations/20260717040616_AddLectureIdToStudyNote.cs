using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureIdToStudyNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LectureId",
                table: "StudyNotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyNotes_LectureId",
                table: "StudyNotes",
                column: "LectureId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyNotes_Lectures_LectureId",
                table: "StudyNotes",
                column: "LectureId",
                principalTable: "Lectures",
                principalColumn: "LectureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyNotes_Lectures_LectureId",
                table: "StudyNotes");

            migrationBuilder.DropIndex(
                name: "IX_StudyNotes_LectureId",
                table: "StudyNotes");

            migrationBuilder.DropColumn(
                name: "LectureId",
                table: "StudyNotes");
        }
    }
}
