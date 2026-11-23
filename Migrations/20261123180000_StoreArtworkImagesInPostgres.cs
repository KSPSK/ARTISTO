using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class StoreArtworkImagesInPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkImages",
                columns: table => new
                {
                    ArtworkListingId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkImages", x => x.ArtworkListingId);
                    table.ForeignKey(
                        name: "FK_ArtworkImages_ArtworkListings_ArtworkListingId",
                        column: x => x.ArtworkListingId,
                        principalTable: "ArtworkListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE "ArtworkListings"
                SET "ImageUrl" = NULL
                WHERE "ImageUrl" LIKE '/uploads/artworks/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ArtworkListings"
                SET "ImageUrl" = NULL
                WHERE "ImageUrl" LIKE '/api/artworklistings/%/image?v=%';
                """);

            migrationBuilder.DropTable(name: "ArtworkImages");
        }
    }
}
