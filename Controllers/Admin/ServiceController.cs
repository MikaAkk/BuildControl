using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class ServiceController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _audit;

    public ServiceController(AuthService auth, AppDbContext db, AuditLogService audit) : base(auth, db)
    {
        _db = db;
        _audit = audit;
    }

    [HttpGet]
    public  async Task<IActionResult> ServiceIndex(
        string? search,
        string? status,
        string? sortBy)
    {
        var query = _db.WorkerServices.AsQueryable();

        if (status == "active")
            query = query.Where(s => s.IsActive);
        else if (status == "inactive")
            query = query.Where(s => !s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(lower) ||
                (s.Description != null && s.Description.ToLower().Contains(lower)));
        }

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(s => s.BasePrice),
            "price_desc" => query.OrderByDescending(s => s.BasePrice),
            "name_asc" => query.OrderBy(s => s.Name),
            _ => query.OrderByDescending(s => s.Id)
        };

        var services = await query.ToListAsync();

        ViewBag.SearchTerm = search ?? "";
        ViewBag.StatusFilter = status ?? "active";
        ViewBag.SortBy = sortBy ?? "";

        return View("~/Views/Admin/ServiceAdmin/ServiceList.cshtml", services);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("~/Views/Admin/ServiceAdmin/ServiceCreate.cshtml", new WorkerService());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WorkerService model)
    {
        if (ModelState.IsValid)
        {
            model.IsActive = true;
            _db.WorkerServices.Add(model);
            await _db.SaveChangesAsync();
            await _audit.LogAsync("Create", "Service", model.Id,
                $"Создана услуга: {model.Name}, цена: {model.BasePrice} {model.Unit}");

            TempData["SuccessMessage"] = "Услуга успешно создана.";
            return RedirectToAction(nameof(ServiceIndex));
        }
        return View("~/Views/Admin/ServiceAdmin/ServiceCreate.cshtml", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var service = await _db.WorkerServices.FindAsync(id);
        if (service == null) return NotFound();
        return View("~/Views/Admin/ServiceAdmin/ServiceCreate.cshtml", service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WorkerService model)
    {
        if (ModelState.IsValid)
        {
            var existing = await _db.WorkerServices.FindAsync(model.Id);
            if (existing == null)
            {
                TempData["ErrorMessage"] = "Услуга не найдена.";
                return RedirectToAction(nameof(ServiceIndex));
            }

            existing.Name = model.Name;
            existing.Description = model.Description;
            existing.Unit = model.Unit;
            existing.BasePrice = model.BasePrice;
            existing.IsActive = model.IsActive;

            await _db.SaveChangesAsync();
            await _audit.LogAsync("Update", "Service", existing.Id,
                $"Обновлена услуга: {existing.Name}. Новая цена: {existing.BasePrice}, статус: {(existing.IsActive ? "Активна" : "Неактивна")}");

            TempData["SuccessMessage"] = "Данные услуги обновлены.";
            return RedirectToAction(nameof(ServiceIndex));
        }
        return View("~/Views/Admin/ServiceAdmin/ServiceCreate.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(long id)
    {
        var service = await _db.WorkerServices.FindAsync(id);
        if (service == null)
        {
            TempData["ErrorMessage"] = "Услуга не найдена.";
            return RedirectToAction(nameof(ServiceIndex));
        }

        bool wasActive = service.IsActive;
        service.IsActive = !service.IsActive;
        await _db.SaveChangesAsync();

        string actionText = service.IsActive ? "активирована" : "деактивирована";
        await _audit.LogAsync("Toggle", "Service", service.Id,
            $"Статус услуги «{service.Name}» изменён: было {wasActive}, стало {service.IsActive}");

        TempData["SuccessMessage"] = $"Услуга «{service.Name}» {actionText}.";
        return RedirectToAction(nameof(ServiceIndex));
    }
}
