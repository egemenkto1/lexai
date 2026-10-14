using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lex.Modules.Decisions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHierarchicalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "decisions",
                table: "court_decisions");

            migrationBuilder.AlterColumn<string>(
                name: "ChamberCode",
                schema: "decisions",
                table: "court_decisions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                schema: "decisions",
                table: "court_decisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChamberName",
                schema: "decisions",
                table: "court_decisions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DecisionYear",
                schema: "decisions",
                table: "court_decisions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_court_decisions_DecisionYear_Category_ChamberName",
                schema: "decisions",
                table: "court_decisions",
                columns: new[] { "DecisionYear", "Category", "ChamberName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_court_decisions_DecisionYear_Category_ChamberName",
                schema: "decisions",
                table: "court_decisions");

            migrationBuilder.DropColumn(
                name: "Category",
                schema: "decisions",
                table: "court_decisions");

            migrationBuilder.DropColumn(
                name: "ChamberName",
                schema: "decisions",
                table: "court_decisions");

            migrationBuilder.DropColumn(
                name: "DecisionYear",
                schema: "decisions",
                table: "court_decisions");

            migrationBuilder.AlterColumn<string>(
                name: "ChamberCode",
                schema: "decisions",
                table: "court_decisions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                schema: "decisions",
                table: "court_decisions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
