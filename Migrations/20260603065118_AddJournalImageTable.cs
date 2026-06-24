using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolio.Migrations
{
    /// <inheritdoc />
    public partial class AddJournalImageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JournalEntry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContentJson = table.Column<string>(type: "TEXT", nullable: false),
                    ContentHtml = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalTag",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalTag", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    LocalFilePath = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalImage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalImage_JournalEntry_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryJournalTag",
                columns: table => new
                {
                    JournalEntriesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    JournalTagsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryJournalTag", x => new { x.JournalEntriesId, x.JournalTagsId });
                    table.ForeignKey(
                        name: "FK_JournalEntryJournalTag_JournalEntry_JournalEntriesId",
                        column: x => x.JournalEntriesId,
                        principalTable: "JournalEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JournalEntryJournalTag_JournalTag_JournalTagsId",
                        column: x => x.JournalTagsId,
                        principalTable: "JournalTag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_Status",
                table: "JournalEntry",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntry_UpdatedAt",
                table: "JournalEntry",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryJournalTag_JournalTagsId",
                table: "JournalEntryJournalTag",
                column: "JournalTagsId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalImage_JournalEntryId",
                table: "JournalImage",
                column: "JournalEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JournalEntryJournalTag");

            migrationBuilder.DropTable(
                name: "JournalImage");

            migrationBuilder.DropTable(
                name: "JournalTag");

            migrationBuilder.DropTable(
                name: "JournalEntry");
        }
    }
}
