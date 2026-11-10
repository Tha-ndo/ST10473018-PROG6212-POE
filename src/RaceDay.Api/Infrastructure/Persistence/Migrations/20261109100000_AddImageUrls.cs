using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceDay.Api.Migrations
{
    /// <summary>Part 3: stores the Azure Blob Storage URLs for event banners and profile pictures.</summary>
    public partial class AddImageUrls : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BannerImageUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProfilePictureUrl", table: "Users");
            migrationBuilder.DropColumn(name: "BannerImageUrl", table: "Events");
        }
    }
}
