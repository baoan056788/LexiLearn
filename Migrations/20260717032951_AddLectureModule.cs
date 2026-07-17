using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LexiLearn.Migrations
{
    /// <inheritdoc />
    public partial class AddLectureModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LectureCourses",
                columns: table => new
                {
                    CourseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureCourses", x => x.CourseId);
                    table.ForeignKey(
                        name: "FK_LectureCourses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lectures",
                columns: table => new
                {
                    LectureId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lectures", x => x.LectureId);
                    table.ForeignKey(
                        name: "FK_Lectures_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lectures_LectureCourses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "LectureCourses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lectures_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LectureSections",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureId = table.Column<int>(type: "int", nullable: false),
                    ParentSectionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HtmlContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AnchorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HeadingLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureSections", x => x.SectionId);
                    table.ForeignKey(
                        name: "FK_LectureSections_LectureSections_ParentSectionId",
                        column: x => x.ParentSectionId,
                        principalTable: "LectureSections",
                        principalColumn: "SectionId");
                    table.ForeignKey(
                        name: "FK_LectureSections_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "LectureId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LectureQuizzes",
                columns: table => new
                {
                    QuizId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LectureId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionA = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionB = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionC = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OptionD = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureQuizzes", x => x.QuizId);
                    table.ForeignKey(
                        name: "FK_LectureQuizzes_LectureSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "LectureSections",
                        principalColumn: "SectionId");
                    table.ForeignKey(
                        name: "FK_LectureQuizzes_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "LectureId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAnnotations",
                columns: table => new
                {
                    AnnotationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LectureId = table.Column<int>(type: "int", nullable: false),
                    SectionId = table.Column<int>(type: "int", nullable: true),
                    StartOffset = table.Column<int>(type: "int", nullable: false),
                    EndOffset = table.Column<int>(type: "int", nullable: false),
                    XPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SelectedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAnnotations", x => x.AnnotationId);
                    table.ForeignKey(
                        name: "FK_UserAnnotations_LectureSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "LectureSections",
                        principalColumn: "SectionId");
                    table.ForeignKey(
                        name: "FK_UserAnnotations_Lectures_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lectures",
                        principalColumn: "LectureId");
                    table.ForeignKey(
                        name: "FK_UserAnnotations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LectureCourses_UserId",
                table: "LectureCourses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizzes_LectureId",
                table: "LectureQuizzes",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureQuizzes_SectionId",
                table: "LectureQuizzes",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_CategoryId",
                table: "Lectures",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_CourseId",
                table: "Lectures",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_IsPublic",
                table: "Lectures",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_UserId_CreatedAt",
                table: "Lectures",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LectureSections_LectureId",
                table: "LectureSections",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureSections_ParentSectionId",
                table: "LectureSections",
                column: "ParentSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnnotations_LectureId",
                table: "UserAnnotations",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnnotations_SectionId",
                table: "UserAnnotations",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnnotations_UserId_LectureId",
                table: "UserAnnotations",
                columns: new[] { "UserId", "LectureId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LectureQuizzes");

            migrationBuilder.DropTable(
                name: "UserAnnotations");

            migrationBuilder.DropTable(
                name: "LectureSections");

            migrationBuilder.DropTable(
                name: "Lectures");

            migrationBuilder.DropTable(
                name: "LectureCourses");
        }
    }
}
