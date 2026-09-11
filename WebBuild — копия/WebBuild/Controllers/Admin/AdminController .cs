using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class AdminController : BaseController
{
    private readonly AppDbContext _db;
    public AdminController(AuthService auth, AppDbContext db) : base(auth)
    {
        _db = db;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();

        if (!roles.Contains("Администратор"))
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Title = "Панель администратора";

        var totalApplications = await _db.Applications.CountAsync();

        var excludedStatusIds = await _db.ApplicationStatuses
            .Where(s => s.Name == "Отклонена" || s.Name == "Выполнена")
            .Select(s => s.Id)
            .ToListAsync();

        var activeCount = excludedStatusIds.Any()
            ? await _db.Applications.Where(a => !excludedStatusIds.Contains(a.StatusId)).CountAsync()
            : totalApplications;

        var newCount = await _db.Applications
            .Where(a => a.CreatedAt >= DateTime.UtcNow.AddHours(-24))
            .CountAsync();

        var totalClients = await _db.Clients.CountAsync();

        var activeEmployeesCount = await _db.Employees
            .IgnoreQueryFilters()
            .Where(e => !e.IsDeleted)
            .CountAsync();
        var activeStaffPreview = await _db.Employees
            .IgnoreQueryFilters()
            .Where(e => !e.IsDeleted)
            .Include(e => e.PersonData)
            .Include(e => e.Role)
            .Take(5)
            .ToListAsync();

        ViewBag.TotalApplications = totalApplications;
        ViewBag.ActiveApplications = activeCount;
        ViewBag.NewApplications24h = newCount;
        ViewBag.TotalClients = totalClients;
        ViewBag.ActiveEmployees = activeEmployeesCount;
        ViewBag.ActiveStaffPreview = activeStaffPreview; 

        return View();
    }

    public IActionResult UsersManagement()
    {
        return RedirectToAction(nameof(EditCreateController.EditIndex), "EditCreate");
    }
}
