using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using WebBuild.Models.ObjectManager;
using WebBuild.Service;

namespace WebBuild.Controllers.ObjectManager;

public class TaskManagerController : ObjectManagerController
{
    private readonly AppDbContext _db;
    public TaskManagerController(AppDbContext db, AuthService auth)
        : base(db, auth)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();
        if (!roles.Contains("Руководитель"))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    [HttpGet]
    public async Task<IActionResult> CreateTask(long objectId)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == 0) return RedirectToAction("Index", "ObjectManager");

        var obj = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.Id == objectId && o.ManagerEmployeeId == employeeId);

        if (obj == null)
        {
            TempData["Error"] = "Нет доступа к этому объекту.";
            return RedirectToAction("MyObjects", "ObjectManager");
        }

        var assignedEmployees = await _db.EmployeeObjectAssignments
            .Where(a => a.ObjectId == objectId && a.RemovedAt == null)
            .Include(a => a.Employee).ThenInclude(e => e.PersonData)
            .Include(a => a.Employee).ThenInclude(e => e.Position)
            .ToListAsync();

        var vm = new CreateTaskViewModel
        {
            ObjectId = obj.Id,
            ObjectAddress = obj.Address,
            AvailableEmployees = assignedEmployees.Select(a => new EmployeeOption
            {
                Id = a.Employee.Id,
                FullName = $"{a.Employee.PersonData.Surname} {a.Employee.PersonData.Name}",
                Position = a.Employee.Position?.Name ?? "Без должности"
            }).ToList()
        };

        return View("~/Views/ObjectManager/TaskManager/CreateTask.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(CreateTaskViewModel model)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == 0) return RedirectToAction("Index", "ObjectManager");

        if (!ModelState.IsValid)
        {
            var assigned = await _db.EmployeeObjectAssignments
                .Where(a => a.ObjectId == model.ObjectId && a.RemovedAt == null)
                .Include(a => a.Employee).ThenInclude(e => e.PersonData)
                .Include(a => a.Employee).ThenInclude(e => e.Position)
                .ToListAsync();

            model.AvailableEmployees = assigned.Select(a => new EmployeeOption
            {
                Id = a.Employee.Id,
                FullName = $"{a.Employee.PersonData.Surname} {a.Employee.PersonData.Name}",
                Position = a.Employee.Position?.Name ?? "-"
            }).ToList();

            return View("~/Views/ObjectManager/CreateTask.cshtml", model);
        }

        var defaultStatus = await _db.WorkTaskStatuses.FirstOrDefaultAsync(s => s.Name == "Не начата");
        if (defaultStatus == null)
        {
            TempData["Error"] = "В базе не найден статус 'Не начата'. Проверьте таблицу task_statuses.";
            return RedirectToAction("ObjectDetails", "ObjectManager", new { id = model.ObjectId });
        }
        DateTime? utcStart = model.PlannedStartDate.HasValue
            ? DateTime.SpecifyKind(model.PlannedStartDate.Value, DateTimeKind.Utc)
            : null;

        DateTime? utcEnd = model.PlannedEndDate.HasValue
            ? DateTime.SpecifyKind(model.PlannedEndDate.Value, DateTimeKind.Utc)
            : null;

        var newTask = new WorkTask
        {
            ObjectId = model.ObjectId,
            EmployeeId = model.EmployeeId,
            Title = model.Title,
            Description = model.Description,
            PlannedStartDate = utcStart,   
            PlannedEndDate = utcEnd,   
            StatusId = defaultStatus.Id,
            CreatedAt = DateTime.UtcNow
        };

        _db.WorkTasks.Add(newTask);
        await _db.SaveChangesAsync();

        _db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = managerId,
            Action = "CREATE_TASK",
            EntityType = "Task",
            EntityId = newTask.Id,
            Details = $"Создана задача: {model.Title}"
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Задача успешно создана!";
        return RedirectToAction("ObjectDetails", "ObjectManager", new { id = model.ObjectId });
    }

    [HttpGet]
    public async Task<IActionResult> EditTask(long id)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == 0) return RedirectToAction("Index", "ObjectManager");

        var task = await _db.WorkTasks
            .Include(t => t.Object)
            .Include(t => t.Status)
            .Include(t => t.Employee).ThenInclude(e => e.PersonData)
            .FirstOrDefaultAsync(t => t.Id == id && t.Object.ManagerEmployeeId == managerId);

        if (task == null)
        {
            TempData["Error"] = "Задача не найдена или у вас нет прав на её редактирование.";
            return RedirectToAction("MyObjects", "ObjectManager");
        }

        var assignedEmployees = await _db.EmployeeObjectAssignments
            .Where(a => a.ObjectId == task.ObjectId && a.RemovedAt == null)
            .Include(a => a.Employee).ThenInclude(e => e.PersonData)
            .Include(a => a.Employee).ThenInclude(e => e.Position)
            .ToListAsync();

        var vm = new EditTaskViewModel
        {
            Id = task.Id,
            ObjectId = task.ObjectId,
            ObjectAddress = task.Object?.Address ?? "",
            EmployeeId = task.EmployeeId,
            Title = task.Title,
            Description = task.Description,
            PlannedStartDate = task.PlannedStartDate,
            PlannedEndDate = task.PlannedEndDate,
            StatusId = task.StatusId,
            CurrentStatusName = task.Status?.Name ?? "",
            AvailableEmployees = assignedEmployees.Select(a => new EmployeeOption
            {
                Id = a.Employee.Id,
                FullName = $"{a.Employee.PersonData.Surname} {a.Employee.PersonData.Name}",
                Position = a.Employee.Position?.Name ?? "-"
            }).ToList(),
            AvailableStatuses = await _db.WorkTaskStatuses
                .AsNoTracking()
                .Select(s => new StatusOption { Id = s.Id, Name = s.Name })
                .ToListAsync()
        };

        return View("~/Views/ObjectManager/TaskManager/EditTask.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTask(EditTaskViewModel model)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == 0) return RedirectToAction("Index", "ObjectManager");

        if (!ModelState.IsValid)
        {
            var assigned = await _db.EmployeeObjectAssignments
                .Where(a => a.ObjectId == model.ObjectId && a.RemovedAt == null)
                .Include(a => a.Employee).ThenInclude(e => e.PersonData)
                .Include(a => a.Employee).ThenInclude(e => e.Position)
                .ToListAsync();

            model.AvailableEmployees = assigned.Select(a => new EmployeeOption
            {
                Id = a.Employee.Id,
                FullName = $"{a.Employee.PersonData.Surname} {a.Employee.PersonData.Name}",
                Position = a.Employee.Position?.Name ?? "-"
            }).ToList();

            model.AvailableStatuses = await _db.WorkTaskStatuses
                .AsNoTracking()
                .Select(s => new StatusOption { Id = s.Id, Name = s.Name })
                .ToListAsync();

            return View("~/Views/ObjectManager/TaskManager/EditTask.cshtml", model);
        }
        var task = await _db.WorkTasks
            .Include(t => t.Object)
            .FirstOrDefaultAsync(t => t.Id == model.Id && t.Object.ManagerEmployeeId == managerId);

        if (task == null)
        {
            TempData["Error"] = "Задача не найдена.";
            return RedirectToAction("MyObjects", "ObjectManager");
        }

        task.EmployeeId = model.EmployeeId;
        task.Title = model.Title;
        task.Description = model.Description;
        task.PlannedStartDate = model.PlannedStartDate.HasValue
            ? DateTime.SpecifyKind(model.PlannedStartDate.Value, DateTimeKind.Utc)
            : null;

        task.PlannedEndDate = model.PlannedEndDate.HasValue
            ? DateTime.SpecifyKind(model.PlannedEndDate.Value, DateTimeKind.Utc)
            : null;

        task.StatusId = model.StatusId;
        task.UpdatedAt = DateTime.UtcNow;
        var doneStatus = await _db.WorkTaskStatuses.FirstOrDefaultAsync(s => s.Name == "Готово");
        if (doneStatus != null && model.StatusId == doneStatus.Id)
        {
            task.ActualEndDate = DateTime.UtcNow;
        }
        else
        {
            task.ActualEndDate = null;
        }

        await _db.SaveChangesAsync();
        _db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = managerId,
            Action = "EDIT_TASK",
            EntityType = "Task",
            EntityId = task.Id,
            Details = $"Изменена задача: {model.Title}"
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Задача обновлена.";
        return RedirectToAction("ObjectDetails", "ObjectManager", new { id = model.ObjectId });
    }

}
