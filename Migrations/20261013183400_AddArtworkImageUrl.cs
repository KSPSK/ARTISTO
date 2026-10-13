using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ArtworkListings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ArtworkListings");
        }
    }
}
