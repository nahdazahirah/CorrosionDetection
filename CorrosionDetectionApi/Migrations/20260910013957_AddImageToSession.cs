using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrosionDetectionApi.Migrations
{
    /// <inheritdoc />
    public partial class AddImageToSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalImageBase64",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalImageBase64",
                table: "DetectionSessions");
        }
    }
}
