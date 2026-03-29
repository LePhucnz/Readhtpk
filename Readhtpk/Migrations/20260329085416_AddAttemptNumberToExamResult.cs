using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Readhtpk.Migrations
{
    /// <inheritdoc />
    public partial class AddAttemptNumberToExamResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "ExamResults",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "ExamResults");
        }
    }
}
