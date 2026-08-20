using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBuild.Models; 
using WebBuild.Models.Admin;
using WebBuild.Models.Enities;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class ApplicationController : Controller
{
    private readonly AppDbContext _db;

    public ApplicationController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> ApplicationsList()
    {
        var managers = await _db.Employees
            .Include(a => a.PersonData)
            .ToListAsync();
        var applications = await _db.Applications
            .Include(a => a.Client)
                .ThenInclude(c => c.PersonData)      
                    .ThenInclude(pd => pd.PhoneNumber)
            .Include(a => a.Status)
            .Include(a => a.AssignedManager)
                .ThenInclude(m => m.PersonData)
            .Include(a => a.ApplicationServices)
                .ThenInclude(a => a.Service)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync();
        var viewModels = applications.Select(a =>
        {
            var firstService = a.ApplicationServices.FirstOrDefault();

            return new ApplicationViewModel
            {
                Id = a.Id,
                ClientName = $"{a.Client.PersonData.Surname} {a.Client.PersonData.Name}",
                Phone = a.Client.PersonData.PhoneNumber?.Phone ?? "Нет телефона",

                Email = a.Client.PersonData.Email,
                CompanyName = a.Client.Contragent?.Name ?? "Частное лицо",
                ServiceName = firstService?.Service.Name ?? "Без услуги",
                Quantity = firstService?.Quantity ?? 0,
                TotalPrice = firstService?.TotalPrice ?? 0,
                CurrentStatusName = a.Status.Name,
                AssignedManagerId = a.AssignedManagerId,
                AssignedManagerName = a.AssignedManager != null
                    ? $"{a.AssignedManager.PersonData.Surname} {a.AssignedManager.PersonData.Name}"
                    : "Не назначен",
                Managers = managers
            };
        }).ToList();

        return View("~/Views/Admin/ApplicationsList.cshtml", viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignManager(long applicationId, long managerId, int newStatusId = 0)
    {
        try
        {
            var application = await _db.Applications
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return NotFound("Заявка не найдена");

            string changeLog = "";
            if (managerId == 0)
            {
                if (application.AssignedManagerId.HasValue)
                {
                    changeLog += "Руководитель снят; ";
                }
                application.AssignedManagerId = null;
            }
            else
            {
                var managerExists = await _db.Employees.AnyAsync(e => e.Id == managerId);
                if (!managerExists) return BadRequest("Неверный ID сотрудника");

                if (application.AssignedManagerId != managerId)
                {
                    changeLog += $"Назначен руководитель ID: {managerId}; ";
                }
                application.AssignedManagerId = managerId;
            }

            if (newStatusId > 0)
            {
                var statusExists = await _db.ApplicationStatuses.AnyAsync(s => s.Id == newStatusId);
                if (statusExists)
                {
                    if (application.StatusId != newStatusId)
                    {
                        changeLog += $"Статус изменен на ID: {newStatusId}; ";
                    }
                    application.StatusId = newStatusId;
                }
                else
                {
                    TempData["ErrorMessage"] = "Неверный ID статуса";
                    return RedirectToAction(nameof(AssignManagerForm), new { id = applicationId });
                }
            }

            int currentAdminId = 1;
            application.UpdatedByEmployeeId = currentAdminId;

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(changeLog))
            {
                var history = new ApplicationStatusHistory
                {
                    ApplicationId = application.Id,
                    StatusId = application.StatusId,
                    ChangedByEmployeeId = currentAdminId,
                    ChangeComment = changeLog.TrimEnd(' ', ';'),
                    ChangedAt = DateTime.UtcNow
                };
                _db.ApplicationStatusHistories.Add(history);
                await _db.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Изменения сохранены!";
            return RedirectToAction(nameof(ApplicationsList));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка: {ex.Message}");
            if (ex.InnerException != null) Debug.WriteLine($"Детали: {ex.InnerException.Message}");

            TempData["ErrorMessage"] = "Ошибка при сохранении: " + (ex.InnerException?.Message ?? ex.Message);
            return RedirectToAction(nameof(AssignManagerForm), new { id = applicationId });
        }
    }

    public async Task<IActionResult> AssignManagerForm(int id)
    {
        var application = await _db.Applications
            .Include(a => a.Client).ThenInclude(c => c.PersonData)
            .Include(a => a.AssignedManager).ThenInclude(a => a.PersonData)
            .Include(a => a.Status)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null) return NotFound();

        var managers = await _db.Employees
            .Include(e => e.PersonData)
            .Include(e => e.Role)
            .Where(a => a.Role != null && a.Role.Name == "Руководитель")
            .ToListAsync();

        var viewModel = new ApplicationViewModel
        {
            Id = application.Id,
            ClientName = $"{application.Client.PersonData.Surname} {application.Client.PersonData.Name}",
            CurrentStatusName = application.Status.Name,
            AssignedManagerId = application.AssignedManagerId,
            Managers = managers
        };

        return View("~/Views/Admin/AssignManagerForm.cshtml", viewModel);
    }
}
