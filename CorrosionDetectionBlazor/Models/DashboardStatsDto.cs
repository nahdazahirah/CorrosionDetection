namespace CorrosionDetection.Blazor.Models
{
    public class DashboardStatsDto
    {
        public int TotalInspections { get; set; }
        public double HealthIndex { get; set; } = 100.0;
        public int CriticalCount { get; set; }
        public int SevereCount { get; set; }
        public int ModerateCount { get; set; }
        public int LowCount { get; set; }
        public int NominalCount { get; set; }
        public int ActiveWorkOrders { get; set; }
        public List<AlertSummaryDto> RecentAlerts { get; set; } = new();
        public List<UnitStatDto> UnitStats { get; set; } = new();
        public List<EquipmentStatDto> EquipmentStats { get; set; } = new();
    }

    public class AlertSummaryDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string? EquipmentTag { get; set; }
        public string? UnitName { get; set; }
        public string? Area { get; set; }
        public string? SeverityGrade { get; set; }
        public string? InspectionMethod { get; set; }
        public float MaxAreaPercentage { get; set; }
        public string? WorkOrderStatus { get; set; }
        public string? WorkOrderNumber { get; set; }
    }

    public class UnitStatDto
    {
        public string Unit { get; set; } = "";
        public int Count { get; set; }
        public int Critical { get; set; }
        public int Severe { get; set; }
    }

    public class EquipmentStatDto
    {
        public string EquipmentType { get; set; } = "";
        public int Count { get; set; }
    }
}
