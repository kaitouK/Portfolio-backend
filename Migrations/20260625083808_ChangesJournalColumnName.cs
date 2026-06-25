using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolio.Migrations
{
    /// <inheritdoc />
    public partial class ChangesJournalColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LocalFilePath",
                table: "JournalImages",
                newName: "BlobName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BlobName",
                table: "JournalImages",
                newName: "LocalFilePath");
        }
    }
}
