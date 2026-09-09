using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrosionDetectionApi.Migrations
{
    /// <inheritdoc />
    public partial class FixDetectionItemSessionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetectionItems_DetectionSessions_DetectionSessionId",
                table: "DetectionItems");

            migrationBuilder.DropIndex(
                name: "IX_DetectionItems_DetectionSessionId",
                table: "DetectionItems");

            migrationBuilder.DropColumn(
                name: "DetectionSessionId",
                table: "DetectionItems");

            migrationBuilder.CreateIndex(
                name: "IX_DetectionItems_SessionId",
                table: "DetectionItems",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetectionItems_DetectionSessions_SessionId",
                table: "DetectionItems",
                column: "SessionId",
                principalTable: "DetectionSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DetectionItems_DetectionSessions_SessionId",
                table: "DetectionItems");

            migrationBuilder.DropIndex(
                name: "IX_DetectionItems_SessionId",
                table: "DetectionItems");

            migrationBuilder.AddColumn<int>(
                name: "DetectionSessionId",
                table: "DetectionItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DetectionItems_DetectionSessionId",
                table: "DetectionItems",
                column: "DetectionSessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DetectionItems_DetectionSessions_DetectionSessionId",
                table: "DetectionItems",
                column: "DetectionSessionId",
                principalTable: "DetectionSessions",
                principalColumn: "Id");
        }
    }
}
