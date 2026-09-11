using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using WebBuild.Service;

namespace WebBuild.Controllers;

public class RealEstateController : BaseController
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _audit;

    public RealEstateController(AppDbContext db, AuthService auth, AuditLogService audit) : base(auth)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> RealEstateIndex(
        string? search,
        long? statusId,
        long? managerId,
        string? sortBy)
    {
        if (!_auth.IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var employeeId = _auth.GetCurrentUserId();
        if (employeeId == null)
            return RedirectToAction("Login", "Account");

        var roles = _auth.GetRoles();
        var isAdmin = roles.Any(r => r.ToLower().Contains("администратор"));
        var isManager = roles.Any(r => r.ToLower().Contains("руководитель"));

        var query = _db.RealEstateObjects
            .Include(o => o.CurrentStatus)
            .Include(o => o.Manager)
                .ThenInclude(m => m.PersonData)
            .AsQueryable();

        if (!isAdmin)
        {
            if (isManager)
            {
                query = query.Where(o => o.ManagerEmployeeId == employeeId);
            }
            else
            {
                query = query.Where(o => o.Tasks.Any(t => t.EmployeeId == employeeId));
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(o =>
                o.Address.ToLower().Contains(lower) ||
                (o.ProjectDescription != null && o.ProjectDescription.ToLower().Contains(lower)));
        }

        if (statusId.HasValue && statusId.Value > 0)
        {
            query = query.Where(o => o.CurrentStatusId == statusId.Value);
        }
        if (isAdmin && managerId.HasValue && managerId.Value > 0)
        {
            query = query.Where(o => o.ManagerEmployeeId == managerId.Value);
        }

        query = sortBy switch
        {
            "address_asc" => query.OrderBy(o => o.Address),
            "address_desc" => query.OrderByDescending(o => o.Address),
            _ => query.OrderByDescending(o => o.Id)
        };

        var objects = await query.ToListAsync();
        ViewBag.Statuses = await _db.ObjectStatuses
            .OrderBy(s => s.Name)
            .ToListAsync();

        if (isAdmin)
        {
            ViewBag.Managers = await _db.Employees
                .Include(e => e.PersonData)
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.PersonData.Surname)
                .Select(e => new
                {
                    e.Id,
                    FullName = e.PersonData.Surname + " " + e.PersonData.Name
                })
                .ToListAsync();
            ViewBag.CanFilterManager = true;
        }
        else
        {
            ViewBag.Managers = new List<object>();
            ViewBag.CanFilterManager = false;
        }

        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsManager = isManager;
        ViewBag.CurrentEmployeeId = employeeId;
        ViewBag.SearchTerm = search ?? "";
        ViewBag.StatusId = statusId ?? 0;
        ViewBag.ManagerId = managerId ?? 0;
        ViewBag.SortBy = sortBy ?? "";

        return View("~/Views/RealEstate/RealEstateIndex.cshtml", objects);
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        if (!_auth.IsAuthenticated())
            return RedirectToAction("Login", "Account");

        var employeeId = _auth.GetCurrentUserId();
        if (employeeId == null)
            return RedirectToAction("Login", "Account");

        var roles = _auth.GetRoles();
        var isAdmin = roles.Any(r => r.ToLower().Contains("администратор"));
        var isManager = roles.Any(r => r.ToLower().Contains("руководитель"));

        var obj = await _db.RealEstateObjects
            .Include(o => o.CurrentStatus)
            .Include(o => o.Manager)
                .ThenInclude(m => m.PersonData)
            .Include(o => o.Contract)
            .Include(o => o.Tasks)
                .ThenInclude(t => t.Status)
            .Include(o => o.Tasks)
                .ThenInclude(t => t.Employee)
                    .ThenInclude(e => e.PersonData)
            .Include(o => o.Documents)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (obj == null) return NotFound();
        if (!isAdmin)
        {
            if (isManager)
            {
                if (obj.ManagerEmployeeId != employeeId)
                    return Forbid();
            }
            else
            {
                var hasTask = obj.Tasks.Any(t => t.EmployeeId == employeeId);
                if (!hasTask)
                    return Forbid();
            }
        }

        ViewBag.IsAdmin = isAdmin;
        ViewBag.IsManager = isManager;
        ViewBag.CurrentEmployeeId = employeeId;

        return View("~/Views/RealEstate/Details.cshtml", obj);
    }
}
    