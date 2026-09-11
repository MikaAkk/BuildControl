using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using WebBuild.Models.ObjectManager;
using WebBuild.Service;

namespace WebBuild.Controllers.ObjectManager;

public class ObjectManagerController : BaseController
{
    private readonly AppDbContext _db;
    public ObjectManagerController(AppDbContext db, AuthService auth) : base(auth)
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

    public async Task<IActionResult> Index()
    {
        ViewBag.Title = "Панель руководителя";
        long? currentUserId = _auth.GetCurrentUserId();

        if (!currentUserId.HasValue)
            return View();

        long? managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == null)
        {
            SetZeroStats(ViewBag);
            ViewBag.TopOverdueTasks = new List<object>(); 
            return View();
        }

        long targetManagerId = managerId.Value;

        try
        {
            var statusArchive = await _db.ObjectStatuses.FirstOrDefaultAsync(s => s.Name == "Архив (завершено)");
            var statusPaused = await _db.ObjectStatuses.FirstOrDefaultAsync(s => s.Name == "Приостановлен");

            ViewBag.TotalObjects = await _db.RealEstateObjects.CountAsync(o => o.ManagerEmployeeId == targetManagerId);

            ViewBag.ArchivedObjects = statusArchive != null
                ? await _db.RealEstateObjects.CountAsync(o => o.ManagerEmployeeId == targetManagerId && o.CurrentStatusId == statusArchive.Id)
                : 0;

            ViewBag.PausedObjects = statusPaused != null
                ? await _db.RealEstateObjects.CountAsync(o => o.ManagerEmployeeId == targetManagerId && o.CurrentStatusId == statusPaused.Id)
                : 0;

            ViewBag.ActiveObjects = ViewBag.TotalObjects - ViewBag.ArchivedObjects - ViewBag.PausedObjects;

            var taskStats = await _db.WorkTasks
                .Where(t => t.Object != null && t.Object.ManagerEmployeeId == targetManagerId)
                .GroupBy(t => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Overdue = g.Count(t => t.PlannedEndDate.HasValue && t.PlannedEndDate < DateTime.UtcNow && t.Status.Name != "Готово" && t.Status.Name != "Отменена"),
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
                ViewBag.TotalTasks = 0; 
            }

            var topOverdue = await _db.WorkTasks
                .Where(t => t.Object != null &&
                            t.Object.ManagerEmployeeId == targetManagerId &&
                            t.PlannedEndDate.HasValue &&
                            t.PlannedEndDate < DateTime.UtcNow &&
                            t.Status.Name != "Готово" &&
                            t.Status.Name != "Отменена")
                .OrderByDescending(t => t.PlannedEndDate)
                .Take(5)
                .Select(t => new
                {
                    ObjectAddress = t.Object.Address,
                    TaskDescription = t.Description ?? "Без описания", 
                    DueDate = t.PlannedEndDate,
                    EmployeeName = t.Employee != null
                        ? $"{t.Employee.PersonData.Surname} {t.Employee.PersonData.Name}"
                        : "Не назначен"
                })
                .ToListAsync();

            ViewBag.TopOverdueTasks = topOverdue;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка дашборда: {ex.Message}");
            SetZeroStats(ViewBag);
            ViewBag.TopOverdueTasks = new List<object>();
        }

        return View();
    }

    private void SetZeroStats(dynamic viewBag)
    {
        viewBag.TotalObjects = 0;
        viewBag.ActiveObjects = 0;
        viewBag.ArchivedObjects = 0;
        viewBag.PausedObjects = 0;
        viewBag.TotalTasks = 0;
        viewBag.OverdueTasks = 0;
        viewBag.InProgressTasks = 0;
        viewBag.DoneTasks = 0;
    }


    public IActionResult ObjectManager()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CreateObject(long contractId)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Template)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c =>
                c.Id == contractId &&
                c.CreatedByEmployeeId == currentEmployeeId.Value);

        if (contract == null)
        {
            TempData["Error"] = "Договор не найден.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        if (contract.Status?.Name != "Подписан")
        {
            TempData["Error"] = "Объект можно создать только по подписанному договору.";
            return RedirectToAction("EditContract", "ContractManager", new { id = contractId });
        }

        var existingObject = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.ContractId == contractId);

        if (existingObject != null)
        {
            TempData["Warning"] = "Объект по этому договору уже существует.";
            return RedirectToAction(nameof(ObjectDetails), new { id = existingObject.Id });
        }

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == 0)
        {
            TempData["Error"] = "Ваша учётная запись сотрудника не найдена.";
            return RedirectToAction("Index");
        }

        var startStatuses = await _db.ObjectStatuses
            .Where(s => s.Name == "Проектирование" || s.Name == "Подготовка площадки")
            .AsNoTracking()
            .Select(s => new StatusOption { Id = s.Id, Name = s.Name })
            .ToListAsync();

        if (!startStatuses.Any())
        {
            var firstStatus = await _db.ObjectStatuses.AsNoTracking().FirstOrDefaultAsync();
            if (firstStatus != null)
                startStatuses.Add(new StatusOption { Id = firstStatus.Id, Name = firstStatus.Name });
        }

        var vm = new CreateObjectViewModel
        {
            ContractId = contractId,
            ClientName = $"{contract.Client.PersonData.Surname} {contract.Client.PersonData.Name}",
            ContractTemplateName = contract.Template?.Name ?? "",
            StatusId = startStatuses.FirstOrDefault()?.Id ?? 0,
            AvailableStatuses = startStatuses
        };

        return View("~/Views/ObjectManager/ObjectManager/CreateObject.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateObject(CreateObjectViewModel model)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c =>
                c.Id == model.ContractId &&
                c.CreatedByEmployeeId == currentEmployeeId.Value);

        if (contract == null)
        {
            TempData["Error"] = "Договор не найден.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        if (contract.Status?.Name != "Подписан")
        {
            TempData["Error"] = "Договор не подписан.";
            return RedirectToAction("EditContract", "ContractManager", new { id = model.ContractId });
        }

        var existingObject = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.ContractId == model.ContractId);

        if (existingObject != null)
        {
            TempData["Warning"] = "Объект уже существует.";
            return RedirectToAction(nameof(ObjectDetails), new { id = existingObject.Id });
        }

        if (!ModelState.IsValid)
        {
            var startStatuses = await _db.ObjectStatuses
                .Where(s => s.Name == "Проектирование" || s.Name == "Подготовка площадки")
                .AsNoTracking()
                .Select(s => new StatusOption { Id = s.Id, Name = s.Name })
                .ToListAsync();

            if (!startStatuses.Any())
            {
                var firstStatus = await _db.ObjectStatuses.AsNoTracking().FirstOrDefaultAsync();
                if (firstStatus != null)
                    startStatuses.Add(new StatusOption { Id = firstStatus.Id, Name = firstStatus.Name });
            }

            model.AvailableStatuses = startStatuses;
            return View("~/Views/ObjectManager/ObjectManager/CreateObject.cshtml", model);
        }

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == 0)
        {
            TempData["Error"] = "Учётная запись сотрудника не найдена.";
            return RedirectToAction("Index");
        }

        var newObject = new RealEstateObject
        {
            Address = model.Address,
            ProjectDescription = model.ProjectDescription,
            CurrentStatusId = model.StatusId,
            ManagerEmployeeId = employeeId,
            ContractId = model.ContractId
        };

        _db.RealEstateObjects.Add(newObject);
        await _db.SaveChangesAsync();

        _db.ManagersHistories.Add(new ManagersHistory
        {
            ObjectId = newObject.Id,
            ManagerEmployeeId = employeeId,
            AssignedByEmployeeId = employeeId,
            StartDate = DateTime.UtcNow
        });

        _db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = employeeId,
            Action = "CREATE_OBJECT",
            EntityType = "Object",
            EntityId = newObject.Id,
            Details = $"Создан объект: {model.Address}"
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = $"Объект №{newObject.Id} создан. Теперь можно назначить сотрудников.";
        return RedirectToAction(nameof(ObjectDetails), new { id = newObject.Id });
    }

    public async Task<IActionResult> MyObjects(string? statusFilter = null)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == 0)
        {
            TempData["Error"] = "Учётная запись сотрудника не найдена.";
            return RedirectToAction("Index");
        }

        var query = _db.RealEstateObjects
            .Include(o => o.CurrentStatus)
            .Include(o => o.Contract).ThenInclude(c => c!.Client).ThenInclude(cl => cl.PersonData)
            .Where(o => o.ManagerEmployeeId == employeeId);

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все")
        {
            query = query.Where(o => o.CurrentStatus.Name == statusFilter);
        }

        var objects = await query
            .OrderByDescending(o => o.Id)
            .ToListAsync();

        var statuses = await _db.ObjectStatuses
            .OrderBy(s => s.Name)
            .AsNoTracking()
            .Select(s => s.Name)
            .ToListAsync();

        ViewBag.Statuses = statuses;
        ViewBag.StatusFilter = statusFilter ?? "Все";

        return View("~/Views/ObjectManager/ObjectManager/MyObjects.cshtml", objects);
    }

    [HttpGet]
    public async Task<IActionResult> ObjectDetails(long id, string? statusFilter = null)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        var obj = await _db.RealEstateObjects
            .Include(o => o.CurrentStatus)
            .Include(o => o.Manager).ThenInclude(m => m!.PersonData)
            .Include(o => o.Contract).ThenInclude(c => c!.Client).ThenInclude(cl => cl.PersonData)
            .Include(o => o.Contract).ThenInclude(c => c!.Status)
            .Include(o => o.EmployeeAssignments).ThenInclude(a => a.Employee).ThenInclude(e => e.PersonData)
            .Include(o => o.EmployeeAssignments).ThenInclude(a => a.Employee).ThenInclude(e => e.Position)
            .Include(o => o.Tasks).ThenInclude(t => t.Status)
            .Include(o => o.Tasks).ThenInclude(t => t.Employee).ThenInclude(e => e.PersonData)
            .FirstOrDefaultAsync(o => o.Id == id && o.ManagerEmployeeId == employeeId);

        if (obj == null)
        {
            TempData["Error"] = "Объект не найден.";
            return RedirectToAction(nameof(MyObjects));
        }

        ViewBag.ObjectStatuses = await _db.ObjectStatuses
            .OrderBy(s => s.Id)
            .AsNoTracking()
            .ToListAsync();
        if (string.IsNullOrEmpty(statusFilter))
        {
            statusFilter = "В работе";
        }

        ViewBag.StatusFilter = statusFilter;

        int totalTasks = obj.Tasks?.Count ?? 0;
        int activeTasks = obj.Tasks?.Count(t => t.Status?.Name == "В работе" || t.Status?.Name == "Требует проверки") ?? 0;
        int doneTasks = obj.Tasks?.Count(t => t.Status?.Name == "Готово") ?? 0;

        ViewBag.TotalTasks = totalTasks;
        ViewBag.ActiveTasks = activeTasks;
        ViewBag.DoneTasks = doneTasks;

        var allTasks = obj.Tasks?.ToList() ?? new List<WorkTask>();

        IEnumerable<WorkTask> filteredTasks = allTasks;

        if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "Все")
        {
            filteredTasks = allTasks.Where(t => t.Status?.Name == statusFilter);
        }

        ViewBag.FilteredTasks = filteredTasks.OrderByDescending(t => t.CreatedAt).ToList();

        ViewBag.TaskStatuses = await _db.WorkTaskStatuses
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();

        return View("~/Views/ObjectManager/ObjectManager/ObjectDetails.cshtml", obj);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeObjectStatus(long objectId, long newStatusId)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        var obj = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.Id == objectId && o.ManagerEmployeeId == managerId);

        if (obj == null)
        {
            TempData["Error"] = "Объект не найден.";
            return RedirectToAction(nameof(MyObjects));
        }

        var statusExists = await _db.ObjectStatuses.AnyAsync(s => s.Id == newStatusId);
        if (!statusExists)
        {
            TempData["Error"] = "Статус не найден.";
            return RedirectToAction(nameof(ObjectDetails), new { id = objectId });
        }

        obj.CurrentStatusId = newStatusId;

        _db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = managerId,
            Action = "CHANGE_OBJECT_STATUS",
            EntityType = "Object",
            EntityId = obj.Id,
            Details = $"Изменён статус объекта на ID: {newStatusId}"
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = "Статус объекта обновлён.";
        return RedirectToAction(nameof(ObjectDetails), new { id = objectId });
    }


    [HttpGet]
    public async Task<IActionResult> AssignEmployees(long id)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var employeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (employeeId == 0)
            return RedirectToAction("Index");

        var obj = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.Id == id && o.ManagerEmployeeId == employeeId);

        if (obj == null)
        {
            TempData["Error"] = "Объект не найден или у вас нет прав.";
            return RedirectToAction(nameof(MyObjects));
        }

        var employees = await _db.Employees
            .Include(e => e.PersonData)
            .Include(e => e.Position)
            .Where(e => !e.IsDeleted && e.Role.Name == "Сотрудник")
            .Select(e => new EmployeeOption
            {
                Id = e.Id,
                FullName = $"{e.PersonData.Surname} {e.PersonData.Name}",
                Position = e.Position != null ? e.Position.Name : "Без должности"
            })
            .ToListAsync();

        var assignedIds = await _db.EmployeeObjectAssignments
            .Where(a => a.ObjectId == id && a.RemovedAt == null)
            .Select(a => a.EmployeeId)
            .ToListAsync();

        var vm = new AssignEmployeesViewModel
        {
            ObjectId = obj.Id,
            ObjectAddress = obj.Address,
            AvailableEmployees = employees,
            AlreadyAssignedIds = assignedIds
        };

        return View("~/Views/ObjectManager/ObjectManager/AssignEmployees.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignEmployees(AssignEmployeesViewModel model)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var managerEmployeeId = await _db.Employees
            .Where(e => e.PeopleId == currentEmployeeId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerEmployeeId == 0)
            return RedirectToAction("Index");

        var selectedIds = model.SelectedEmployeeIds ?? new List<long>();

        var currentAssignments = await _db.EmployeeObjectAssignments
            .Where(a => a.ObjectId == model.ObjectId && a.RemovedAt == null)
            .ToListAsync();
        var toRemove = currentAssignments
            .Where(a => !selectedIds.Contains(a.EmployeeId));

        foreach (var rem in toRemove)
        {
            rem.RemovedAt = DateTime.UtcNow;
        }
        foreach (var empId in selectedIds)
        {
            var exists = currentAssignments.Any(a => a.EmployeeId == empId);
            if (!exists)
            {
                _db.EmployeeObjectAssignments.Add(new EmployeeObjectAssignment
                {
                    ObjectId = model.ObjectId,
                    EmployeeId = empId,
                    AssignedByEmployeeId = managerEmployeeId,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Назначения сотрудников обновлены.";
        return RedirectToAction(nameof(ObjectDetails), new { id = model.ObjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseObject(long objectId)
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == 0) return RedirectToAction("Index", "ObjectManager");

        var obj = await _db.RealEstateObjects
            .FirstOrDefaultAsync(o => o.Id == objectId && o.ManagerEmployeeId == managerId);

        if (obj == null)
        {
            TempData["Error"] = "Объект не найден.";
            return RedirectToAction(nameof(MyObjects));
        }

        var archiveStatus = await _db.ObjectStatuses
            .FirstOrDefaultAsync(s => s.Name == "Архив (завершено)");

        if (archiveStatus == null)
        {
            archiveStatus = new ObjectStatus { Name = "Архив (завершено)" };
            _db.ObjectStatuses.Add(archiveStatus);
            await _db.SaveChangesAsync();
        }

        obj.CurrentStatusId = archiveStatus.Id;
        _db.AuditLogs.Add(new AuditLog
        {
            EmployeeId = managerId,
            Action = "CLOSE_OBJECT",
            EntityType = "Object",
            EntityId = obj.Id,
            Details = $"Объект переведён в статус 'Архив (завершено)'"
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = "Объект успешно закрыт и перемещён в архив.";
        return RedirectToAction(nameof(ObjectDetails), new { id = objectId });
    }
}
