using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ARTISTO.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatorProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatorProfileId",
                table: "ArtworkListings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreatorProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreatorProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArtworkListings_CreatorProfileId",
                table: "ArtworkListings",
                column: "CreatorProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ArtworkListings_CreatorProfiles_CreatorProfileId",
                table: "ArtworkListings",
                column: "CreatorProfileId",
                principalTable: "CreatorProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArtworkListings_CreatorProfiles_CreatorProfileId",
                table: "ArtworkListings");

            migrationBuilder.DropTable(
                name: "CreatorProfiles");

            migrationBuilder.DropIndex(
                name: "IX_ArtworkListings_CreatorProfileId",
                table: "ArtworkListings");

            migrationBuilder.DropColumn(
                name: "CreatorProfileId",
                table: "ArtworkListings");
        }
    }
}
