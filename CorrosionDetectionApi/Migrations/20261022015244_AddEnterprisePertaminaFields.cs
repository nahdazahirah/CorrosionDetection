using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CorrosionDetectionApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterprisePertaminaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Area",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentTag",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentType",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectionMethod",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectorName",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InspectorNip",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeverityGrade",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitName",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderNumber",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WorkOrderStatus",
                table: "DetectionSessions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "EquipmentTag",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "EquipmentType",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "InspectionMethod",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "InspectorName",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "InspectorNip",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "SeverityGrade",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "UnitName",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "WorkOrderNumber",
                table: "DetectionSessions");

            migrationBuilder.DropColumn(
                name: "WorkOrderStatus",
                table: "DetectionSessions");
        }
    }
}
