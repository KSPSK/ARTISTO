using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivateArtworkFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkFavorites",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ArtworkListingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkFavorites", x => new { x.UserId, x.ArtworkListingId });
                    table.ForeignKey(
                        name: "FK_ArtworkFavorites_ArtworkListings_ArtworkListingId",
                        column: x => x.ArtworkListingId,
                        principalTable: "ArtworkListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkFavorites_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkFavorites_ArtworkListingId",
                table: "ArtworkFavorites",
                column: "ArtworkListingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkFavorites");
        }
    }
}
