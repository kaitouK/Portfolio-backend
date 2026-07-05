using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyPortfolio.Migrations
{
    /// <inheritdoc />
    public partial class ConvertDateTimeOffsetToDateTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            UPDATE JournalEntries 
            SET CreatedAt = substr(CreatedAt, 1, 19)
            WHERE CreatedAt IS NOT NULL;
        ");
            migrationBuilder.Sql(@"
            UPDATE JournalEntries 
            SET UpdatedAt = substr(UpdatedAt, 1, 19)
            WHERE UpdatedAt IS NOT NULL;
        ");
            migrationBuilder.Sql(@"
            UPDATE JournalImages 
            SET CreatedAt = substr(CreatedAt, 1, 19)
            WHERE CreatedAt IS NOT NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "JournalEntries",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "JournalImages",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                        name: "CreatedAt",
                        table: "JournalEntries",
                        type: "TEXT",
                        nullable: false,
                        oldClrType: typeof(DateTime),
                        oldType: "TEXT");
            migrationBuilder.AlterColumn<DateTimeOffset>(
                        name: "CreatedAt",
                        table: "JournalImages",
                        type: "TEXT",
                        nullable: false,
                        oldClrType: typeof(DateTime),
                        oldType: "TEXT");
            migrationBuilder.AlterColumn<DateTimeOffset>(
                        name: "UpdatedAt",
                        table: "JournalEntries",
                        type: "TEXT",
                        nullable: false,
                        oldClrType: typeof(DateTime),
                        oldType: "TEXT");


        }
    }
}
