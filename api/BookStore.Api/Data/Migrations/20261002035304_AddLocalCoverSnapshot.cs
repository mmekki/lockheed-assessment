using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalCoverSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoverContentType",
                table: "Books",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "CoverImage",
                table: "Books",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverContentType",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Books");
        }
    }
}
