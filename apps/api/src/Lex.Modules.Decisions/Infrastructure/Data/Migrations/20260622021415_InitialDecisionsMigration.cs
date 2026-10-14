using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lex.Modules.Decisions.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDecisionsMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "decisions");

            migrationBuilder.CreateTable(
                name: "court_decisions",
                schema: "decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourtType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChamberCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DecisionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RawTextLocation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmbeddingStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_court_decisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "decision_chunks",
                schema: "decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    ChunkText = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    QdrantPointId = table.Column<Guid>(type: "uuid", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EmbeddingVersion = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_decision_chunks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_court_decisions_ChamberCode_CaseNumber_DecisionNumber",
                schema: "decisions",
                table: "court_decisions",
                columns: new[] { "ChamberCode", "CaseNumber", "DecisionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_court_decisions_DecisionDate",
                schema: "decisions",
                table: "court_decisions",
                column: "DecisionDate");

            migrationBuilder.CreateIndex(
                name: "IX_court_decisions_EmbeddingStatus",
                schema: "decisions",
                table: "court_decisions",
                column: "EmbeddingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_decision_chunks_DecisionId",
                schema: "decisions",
                table: "decision_chunks",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_decision_chunks_DecisionId_ChunkIndex",
                schema: "decisions",
                table: "decision_chunks",
                columns: new[] { "DecisionId", "ChunkIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "court_decisions",
                schema: "decisions");

            migrationBuilder.DropTable(
                name: "decision_chunks",
                schema: "decisions");
        }
    }
}
