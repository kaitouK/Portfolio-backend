using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolio.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryJournalTag_JournalEntry_JournalEntriesId",
                table: "JournalEntryJournalTag");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryJournalTag_JournalTag_JournalTagsId",
                table: "JournalEntryJournalTag");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalImage_JournalEntry_JournalEntryId",
                table: "JournalImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalTag",
                table: "JournalTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalImage",
                table: "JournalImage");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalEntry",
                table: "JournalEntry");

            migrationBuilder.RenameTable(
                name: "JournalTag",
                newName: "JournalTags");

            migrationBuilder.RenameTable(
                name: "JournalImage",
                newName: "JournalImages");

            migrationBuilder.RenameTable(
                name: "JournalEntry",
                newName: "JournalEntries");

            migrationBuilder.RenameIndex(
                name: "IX_JournalImage_JournalEntryId",
                table: "JournalImages",
                newName: "IX_JournalImages_JournalEntryId");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntry_UpdatedAt",
                table: "JournalEntries",
                newName: "IX_JournalEntries_UpdatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntry_Status",
                table: "JournalEntries",
                newName: "IX_JournalEntries_Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalTags",
                table: "JournalTags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalImages",
                table: "JournalImages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalEntries",
                table: "JournalEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryJournalTag_JournalEntries_JournalEntriesId",
                table: "JournalEntryJournalTag",
                column: "JournalEntriesId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryJournalTag_JournalTags_JournalTagsId",
                table: "JournalEntryJournalTag",
                column: "JournalTagsId",
                principalTable: "JournalTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalImages_JournalEntries_JournalEntryId",
                table: "JournalImages",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryJournalTag_JournalEntries_JournalEntriesId",
                table: "JournalEntryJournalTag");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryJournalTag_JournalTags_JournalTagsId",
                table: "JournalEntryJournalTag");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalImages_JournalEntries_JournalEntryId",
                table: "JournalImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalTags",
                table: "JournalTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalImages",
                table: "JournalImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JournalEntries",
                table: "JournalEntries");

            migrationBuilder.RenameTable(
                name: "JournalTags",
                newName: "JournalTag");

            migrationBuilder.RenameTable(
                name: "JournalImages",
                newName: "JournalImage");

            migrationBuilder.RenameTable(
                name: "JournalEntries",
                newName: "JournalEntry");

            migrationBuilder.RenameIndex(
                name: "IX_JournalImages_JournalEntryId",
                table: "JournalImage",
                newName: "IX_JournalImage_JournalEntryId");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntries_UpdatedAt",
                table: "JournalEntry",
                newName: "IX_JournalEntry_UpdatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_JournalEntries_Status",
                table: "JournalEntry",
                newName: "IX_JournalEntry_Status");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalTag",
                table: "JournalTag",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalImage",
                table: "JournalImage",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JournalEntry",
                table: "JournalEntry",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryJournalTag_JournalEntry_JournalEntriesId",
                table: "JournalEntryJournalTag",
                column: "JournalEntriesId",
                principalTable: "JournalEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryJournalTag_JournalTag_JournalTagsId",
                table: "JournalEntryJournalTag",
                column: "JournalTagsId",
                principalTable: "JournalTag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalImage_JournalEntry_JournalEntryId",
                table: "JournalImage",
                column: "JournalEntryId",
                principalTable: "JournalEntry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
