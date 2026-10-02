using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    PublishedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    CoverUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OpenLibraryKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 5000, nullable: true),
                    Genre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PriceInCents = table.Column<long>(type: "INTEGER", nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.CheckConstraint("CK_Books_Price", "PriceInCents >= 0");
                    table.CheckConstraint("CK_Books_Stock", "Stock IS NULL OR Stock >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Books_OpenLibraryKey",
                table: "Books",
                column: "OpenLibraryKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
