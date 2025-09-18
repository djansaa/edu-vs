using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduVS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_student_classes_class_id",
                table: "class_student");

            migrationBuilder.DropForeignKey(
                name: "FK_class_student_students_student_id",
                table: "class_student");

            migrationBuilder.DropForeignKey(
                name: "FK_tests_subject_subject_code",
                table: "tests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tests",
                table: "tests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_students",
                table: "students");

            migrationBuilder.DropPrimaryKey(
                name: "PK_classes",
                table: "classes");

            migrationBuilder.RenameTable(
                name: "tests",
                newName: "test");

            migrationBuilder.RenameTable(
                name: "students",
                newName: "student");

            migrationBuilder.RenameTable(
                name: "classes",
                newName: "class");

            migrationBuilder.RenameIndex(
                name: "IX_tests_subject_code",
                table: "test",
                newName: "IX_test_subject_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_test",
                table: "test",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_student",
                table: "student",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_class",
                table: "class",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_student_class_class_id",
                table: "class_student",
                column: "class_id",
                principalTable: "class",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_class_student_student_student_id",
                table: "class_student",
                column: "student_id",
                principalTable: "student",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_test_subject_subject_code",
                table: "test",
                column: "subject_code",
                principalTable: "subject",
                principalColumn: "code",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_student_class_class_id",
                table: "class_student");

            migrationBuilder.DropForeignKey(
                name: "FK_class_student_student_student_id",
                table: "class_student");

            migrationBuilder.DropForeignKey(
                name: "FK_test_subject_subject_code",
                table: "test");

            migrationBuilder.DropPrimaryKey(
                name: "PK_test",
                table: "test");

            migrationBuilder.DropPrimaryKey(
                name: "PK_student",
                table: "student");

            migrationBuilder.DropPrimaryKey(
                name: "PK_class",
                table: "class");

            migrationBuilder.RenameTable(
                name: "test",
                newName: "tests");

            migrationBuilder.RenameTable(
                name: "student",
                newName: "students");

            migrationBuilder.RenameTable(
                name: "class",
                newName: "classes");

            migrationBuilder.RenameIndex(
                name: "IX_test_subject_code",
                table: "tests",
                newName: "IX_tests_subject_code");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tests",
                table: "tests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_students",
                table: "students",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_classes",
                table: "classes",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_class_student_classes_class_id",
                table: "class_student",
                column: "class_id",
                principalTable: "classes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_class_student_students_student_id",
                table: "class_student",
                column: "student_id",
                principalTable: "students",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tests_subject_subject_code",
                table: "tests",
                column: "subject_code",
                principalTable: "subject",
                principalColumn: "code",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
