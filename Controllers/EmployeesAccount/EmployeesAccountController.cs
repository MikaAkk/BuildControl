using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebBuild.Controllers;
using WebBuild.Models;
using WebBuild.Models.EmployeesModel;
using WebBuild.Service;

public class EmployeesAccountController : BaseController
{
    private readonly AppDbContext _db;
    public EmployeesAccountController(AppDbContext db, AuthService auth) : base(auth)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();
        if (!roles.Contains("Сотрудник"))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        long? employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => (long?)e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == null)
        {
            SetZeroStats(ViewBag);
            ViewBag.UpcomingTasks = new List<object>();
            ViewBag.TopOverdueTasks = new List<object>();
            return View();
        }

        long targetEmployeeId = employeeId.Value;

        try
        {
            ViewBag.TotalObjects = await _db.WorkTasks
                .Where(t => t.EmployeeId == targetEmployeeId)
                .Select(t => t.ObjectId)
                .Distinct()
                .CountAsync();

            var taskStats = await _db.WorkTasks
                .Where(t => t.EmployeeId == targetEmployeeId)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Overdue = g.Count(t =>
                        t.PlannedEndDate.HasValue &&
                        t.PlannedEndDate < DateTime.UtcNow &&
                        t.Status.Name != "Готово" &&
                        t.Status.Name != "Отменена"),
                    InProgress = g.Count(t => t.Status.Name == "В работе"),
                    Done = g.Count(t => t.Status.Name == "Готово")
                })
                .FirstOrDefaultAsync();

            if (taskStats != null)
            {
                ViewBag.TotalTasks = taskStats.Total;
                ViewBag.OverdueTasks = taskStats.Overdue;
                ViewBag.InProgressTasks = taskStats.InProgress;
                ViewBag.DoneTasks = taskStats.Done;
            }
            else
            {
                SetZeroStats(ViewBag);
            }

            var topTasksRaw = await _db.WorkTasks
                .AsNoTracking()
                .Include(t => t.Object)
                .Include(t => t.Status)
                .Where(t => t.EmployeeId == targetEmployeeId &&
                            t.Status.Name != "Готово" &&
                            t.Status.Name != "Отменена")
                .OrderBy(t => t.PlannedEndDate ?? DateTime.MaxValue)
                .Take(5)
                .ToListAsync();

            ViewBag.UpcomingTasks = topTasksRaw.Select(t => new TaskSummaryViewModel
            {
                Id = t.Id,
                ObjectAddress = t.Object != null ? t.Object.Address : "—",
                Description = t.Description ?? string.Empty,
                PlannedEndDate = t.PlannedEndDate,
                StatusName = t.Status != null ? t.Status.Name : string.Empty
            }).ToList();

            var overdueTasksRaw = await _db.WorkTasks
                .AsNoTracking()
                .Include(t => t.Object)
                .Include(t => t.Status)
                .Include(t => t.Employee).ThenInclude(e => e.PersonData)
                .Where(t => t.EmployeeId == targetEmployeeId &&
                            t.PlannedEndDate.HasValue &&
                            t.PlannedEndDate < DateTime.UtcNow &&
                            t.Status.Name != "Готово" &&
                            t.Status.Name != "Отменена")
                .OrderByDescending(t => t.PlannedEndDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TopOverdueTasks = overdueTasksRaw.Select(t => new
            {
                ObjectAddress = t.Object != null ? t.Object.Address : "—",
                TaskDescription = t.Description ?? "Без описания",
                DueDate = t.PlannedEndDate.Value,
                EmployeeName = t.Employee != null
                    ? $"{t.Employee.PersonData.Surname} {t.Employee.PersonData.Name}"
                    : "—"
            }).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка дашборда сотрудника: {ex.Message}");
            SetZeroStats(ViewBag);
            ViewBag.UpcomingTasks = new List<object>();
            ViewBag.TopOverdueTasks = new List<object>();
        }

        return View();
    }


    [HttpGet]
    public async Task<IActionResult> MyTasks()
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        long? employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => (long?)e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == null)
        {
            ViewBag.Error = "Учётная запись сотрудника не найдена.";
            return View();
        }

        long targetEmployeeId = employeeId.Value;

        var tasks = await _db.WorkTasks
            .AsNoTracking()
            .Include(t => t.Object)
            .Include(t => t.Status)
            .Where(t => t.EmployeeId == targetEmployeeId)
            .OrderByDescending(t => t.PlannedEndDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        var availableStatuses = await _db.WorkTaskStatuses
            .Where(s => s.Name != "Отменена")
            .ToListAsync();

        var viewModel = new EmployeeTasksPageViewModel
        {
            Tasks = tasks.Select(t => new EmployeeTaskListViewModel
            {
                Id = t.Id,
                Description = t.Description ?? string.Empty,
                ObjectAddress = t.Object != null ? t.Object.Address : "—",
                PlannedEndDate = t.PlannedEndDate,
                StatusId = t.StatusId,
                StatusName = t.Status != null ? t.Status.Name : string.Empty,
                IsOverdue = t.PlannedEndDate.HasValue &&
                            t.PlannedEndDate < DateTime.UtcNow &&
                            t.Status != null &&
                            t.Status.Name != "Готово"
            }).ToList(),
            AvailableStatuses = availableStatuses
        };

        return View(viewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(long taskId, long newStatusId)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue)
            return RedirectToAction("Login", "Account");

        long? employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => (long?)e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == null)
            return Unauthorized();

        var task = await _db.WorkTasks
            .Include(t => t.Status)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.EmployeeId == employeeId);

        if (task == null)
            return NotFound("Задача не найдена или не назначена вам.");

        var statusToSet = await _db.WorkTaskStatuses.FindAsync(newStatusId);
        if (statusToSet == null || statusToSet.Name == "Отменена")
            return BadRequest("Недопустимый статус задачи.");

        task.StatusId = newStatusId;

        if (statusToSet.Name == "Готово")
        {
            task.CreatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Статус задачи «{task.Description}» обновлён на «{statusToSet.Name}».";
        return RedirectToAction(nameof(MyTasks));
    }


    private static (string badgeClass, string daysLeftText) ComputeBadgeInfo(
        DateTime? plannedEndDate,
        string? statusName)
    {
        if (!plannedEndDate.HasValue)
            return ("bg-secondary", "Без срока");

        if (statusName == "Готово" || statusName == "Отменена")
            return ("bg-secondary", "—");

        var daysLeft = (plannedEndDate.Value.Date - DateTime.UtcNow.Date).Days;

        if (daysLeft < 0)
            return ("bg-danger", $"Просрочено на {Math.Abs(daysLeft)} дн.");
        if (daysLeft == 0)
            return ("bg-danger", "Сегодня");
        if (daysLeft <= 3)
            return ("bg-warning text-dark", $"{daysLeft} дн.");
        return ("bg-success", $"{daysLeft} дн.");
    }

    private void SetZeroStats(dynamic bag)
    {
        bag.TotalObjects = 0;
        bag.TotalTasks = 0;
        bag.InProgressTasks = 0;
        bag.DoneTasks = 0;
        bag.OverdueTasks = 0;
    }
}
