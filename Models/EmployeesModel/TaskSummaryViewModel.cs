namespace WebBuild.Models.EmployeesModel;

public class TaskSummaryViewModel
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ObjectAddress { get; set; } = "—";
    public DateTime? PlannedEndDate { get; set; }
    public string StatusName { get; set; } = string.Empty;

    public int DaysLeft
    {
        get
        {
            if (!PlannedEndDate.HasValue) return int.MaxValue; 

            var moscowTZ = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, moscowTZ);
            return (PlannedEndDate.Value.Date - nowLocal.Date).Days;
        }
    }

    public string DaysLeftBadge
    {
        get
        {
            if (!PlannedEndDate.HasValue) return "—";
            if (DaysLeft < 0) return $"Просрочено на {Math.Abs(DaysLeft)} дн.";
            if (DaysLeft == 0) return "Сегодня";
            if (DaysLeft <= 2) return $"{DaysLeft} дн. осталось";
            return $"{DaysLeft} дн.";
        }
    }
    public string BadgeClass => DaysLeft switch
    {
        < 0 => "bg-danger",
        0 or 1 or 2 => "bg-warning",
        _ => "bg-info"
    };
}