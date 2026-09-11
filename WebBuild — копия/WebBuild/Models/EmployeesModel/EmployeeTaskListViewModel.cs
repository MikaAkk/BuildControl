namespace WebBuild.Models.EmployeesModel;

public class EmployeeTaskListViewModel
{
    public long Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ObjectAddress { get; set; } = "—";
    public DateTime? PlannedEndDate { get; set; }
    public long StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
    private static readonly TimeZoneInfo MoscowTZ = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    private static DateTime NowLocal => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, MoscowTZ);

    public string DaysLeftText
    {
        get
        {
            if (!PlannedEndDate.HasValue) return "Без срока";

            var daysLeft = (PlannedEndDate.Value.Date - NowLocal.Date).Days;

            if (daysLeft < 0) return $"Просрочено на {Math.Abs(daysLeft)} дн.";
            if (daysLeft == 0) return "Сегодня";
            if (daysLeft <= 2) return $"{daysLeft} дн. осталось";

            return $"{daysLeft} дн.";
        }
    }

    public string BadgeClass
    {
        get
        {
            if (!PlannedEndDate.HasValue) return "bg-secondary";

            var daysLeft = (PlannedEndDate.Value.Date - NowLocal.Date).Days;

            if (daysLeft < 0) return "bg-danger";
            if (daysLeft <= 2) return "bg-warning";

            return "bg-info";
        }
    }
}