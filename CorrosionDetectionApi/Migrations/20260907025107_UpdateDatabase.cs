using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrosionDetectionApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectionSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageWidth = table.Column<int>(type: "int", nullable: false),
                    ImageHeight = table.Column<int>(type: "int", nullable: false),
                    DetectionCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Width = table.Column<float>(type: "real", nullable: false),
                    Height = table.Column<float>(type: "real", nullable: false),
                    Confidence = table.Column<float>(type: "real", nullable: false),
                    AreaPercentage = table.Column<float>(type: "real", nullable: false),
                    MaskImageBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectionSessionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectionItems_DetectionSessions_DetectionSessionId",
                        column: x => x.DetectionSessionId,
                        principalTable: "DetectionSessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectionItems_DetectionSessionId",
                table: "DetectionItems",
                column: "DetectionSessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectionItems");

            migrationBuilder.DropTable(
                name: "DetectionSessions");
        }
    }
}
