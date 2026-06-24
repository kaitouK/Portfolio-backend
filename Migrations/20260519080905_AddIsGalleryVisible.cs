using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MyPortfolio.Migrations
{
    /// <inheritdoc />
    public partial class AddIsGalleryVisible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Tags",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsGalleryVisible",
                table: "Artworks",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "TagId", "TagName", "Type" },
                values: new object[,]
                {
                    { 1, "正面", 1 },
                    { 2, "1/4側面", 1 },
                    { 3, "半側面", 1 },
                    { 4, "3/4側面", 1 },
                    { 5, "正側面", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagId",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagId",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsGalleryVisible",
                table: "Artworks");
        }
    }
}
