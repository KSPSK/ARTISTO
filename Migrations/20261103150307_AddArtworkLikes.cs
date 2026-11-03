using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddArtworkLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArtworkLikes",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ArtworkListingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtworkLikes", x => new { x.UserId, x.ArtworkListingId });
                    table.ForeignKey(
                        name: "FK_ArtworkLikes_ArtworkListings_ArtworkListingId",
                        column: x => x.ArtworkListingId,
                        principalTable: "ArtworkListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtworkLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkLikes_ArtworkListingId",
                table: "ArtworkLikes",
                column: "ArtworkListingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArtworkLikes");
        }
    }
}
