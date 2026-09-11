using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBuild.Models; 
using WebBuild.Models.Admin;
using WebBuild.Models.Enities;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class ApplicationController : AdminController
{
    private readonly AppDbContext _db;
    private readonly ApplicationHistoryService _historyService;
    private readonly AuditLogService _audit;
    public ApplicationController(AuthService auth, AppDbContext db,  ApplicationHistoryService historyService, AuditLogService audit) : base(auth, db)
    {
        _db = db;
        _historyService = historyService;
        _audit = audit;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkAssignManager(long? managerId, List<long> selectedIds)
    {

        if (managerId == null || managerId == 0 || selectedIds == null || !selectedIds.Any())
        {
            TempData["ErrorMessage"] = "Выберите менеджера и хотя бы одну заявку.";
            return RedirectToAction(nameof(ApplicationsList));
        }

        var currentUserId = _auth.GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var applications = await _db.Applications
            .Where(a => selectedIds.Contains(a.Id))
            .ToListAsync();

        int count = 0;

        foreach (var app in applications)
        {
            app.AssignedManagerId = managerId.Value; 
            app.UpdatedByEmployeeId = currentUserId.Value;

            await _audit.LogAsync("BulkUpdate", "Application", app.Id,
                $"Массовое назначение: менеджер ID {managerId.Value} назначен на заявку #{app.Id}");

            count++;
        }

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Успешно назначено: {count} заявок.";

        return RedirectToAction(nameof(ApplicationsList));
    }
    public async Task<IActionResult> ApplicationsList(
     string? search,
     long? statusId,
     long? managerId,
     int page = 1,
     int pageSize = 20)
    {
        var managers = await _db.Employees
            .Include(e => e.PersonData)
            .Include(e => e.Role)
            .Where(e => e.Role.Name == "Руководитель" && !e.IsDeleted)
            .ToListAsync();

        var statuses = await _db.ApplicationStatuses.ToListAsync();

        var query = _db.Applications
            .Include(a => a.Client).ThenInclude(c => c.PersonData).ThenInclude(pd => pd.PhoneNumber)
            .Include(a => a.Status)
            .Include(a => a.AssignedManager).ThenInclude(m => m.PersonData)
            .Include(a => a.ApplicationServices).ThenInclude(asv => asv.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(a =>
                (a.Client.PersonData.Surname + " " + a.Client.PersonData.Name).ToLower().Contains(lower) ||
                a.Client.PersonData.PhoneNumber.Phone.Contains(search) ||
                a.Client.PersonData.Email.ToLower().Contains(lower) ||
                a.Id.ToString().Contains(search)
            );
        }

        if (statusId.HasValue && statusId.Value > 0)
            query = query.Where(a => a.StatusId == statusId.Value);

        if (managerId.HasValue && managerId.Value > 0)
            query = query.Where(a => a.AssignedManagerId == managerId.Value);

        query = query.OrderByDescending(a => a.CreatedAt);
        int totalItems = await query.CountAsync();

        int skipCount = (page - 1) * pageSize;
        var applications = await query.Skip(skipCount).Take(pageSize).ToListAsync();

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
                CurrentStatusId = a.StatusId,
                CurrentStatusName = a.Status.Name,
                AssignedManagerId = a.AssignedManagerId,
                AssignedManagerName = a.AssignedManager != null
                    ? $"{a.AssignedManager.PersonData.Surname} {a.AssignedManager.PersonData.Name}" 
                    : "Не назначен",
                Managers = managers
            };
        }).ToList();

        ViewBag.Statuses = statuses;
        ViewBag.Managers = managers;
        ViewBag.SelectedStatusId = statusId ?? 0;
        ViewBag.SelectedManagerId = managerId ?? 0;
        ViewBag.SearchTerm = search ?? "";

        ViewBag.TotalItems = totalItems;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.PageSize = pageSize;

        return View("~/Views/Admin/ApplicationAdmin/ApplicationsList.cshtml", viewModels);
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
                    changeLog += $"Назначен руководитель: {managerId}; ";
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
            }

            long? userId = _auth.GetCurrentUserId();
            if (userId == null || userId == 0)
            {
                TempData["ErrorMessage"] = "Ошибка авторизации";
                return RedirectToAction(nameof(ApplicationsList));
            }
            long currentAdminId = userId.Value;
            application.UpdatedByEmployeeId = currentAdminId;

            await _db.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(changeLog))
            {
                await _historyService.LogChangeAsync(
                    applicationId: application.Id,
                    employeeId: currentAdminId,
                    newStatusId: application.StatusId,
                    comment: changeLog.TrimEnd(' ', ';')
                );

                await _audit.LogAsync(
                    "Update",
                    "Application",
                    application.Id,
                    $"Назначение менеджера/смена статуса: {changeLog.TrimEnd(' ', ';')}"
                );
            }

            TempData["SuccessMessage"] = "Изменения сохранены!";
            return RedirectToAction(nameof(ApplicationsList));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка: {ex.Message}");
            TempData["ErrorMessage"] = "Ошибка при сохранении";
            return RedirectToAction(nameof(AssignManagerForm), new { id = applicationId });
        }
    }


    [HttpGet]
    public async Task<IActionResult> AssignManagerForm(long id)
    {
        var application = await _db.Applications
            .Include(a => a.Client).ThenInclude(c => c.PersonData)
            .Include(a => a.AssignedManager).ThenInclude(m => m.PersonData)
            .Include(a => a.Status)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null) return NotFound("Заявка не найдена");
        var managers = await _db.Employees
            .Include(e => e.PersonData)
            .Include(e => e.Role)
            .Where(e => e.Role.Name == "Руководитель" && !e.IsDeleted)
            .ToListAsync();
        var statuses = await _db.ApplicationStatuses.ToListAsync();

        var viewModel = new ApplicationViewModel
        {
            Id = application.Id,
            ClientName = $"{application.Client.PersonData.Surname} {application.Client.PersonData.Name}",
            Phone = application.Client.PersonData.PhoneNumber?.Phone ?? "Нет телефона",
            Email = application.Client.PersonData.Email,
            CompanyName = application.Client.Contragent?.Name ?? "Частное лицо",
            CurrentStatusId = application.StatusId,
            CurrentStatusName = application.Status.Name,

            AssignedManagerId = application.AssignedManagerId,
            AssignedManagerName = application.AssignedManager != null
                ? $"{application.AssignedManager.PersonData.Surname} {application.AssignedManager.PersonData.Name}"
                : "Не назначен",

            Managers = managers
        };

        ViewBag.Statuses = statuses; 

        return View("~/Views/Admin/ApplicationAdmin/AssignManagerForm.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectApplication(long applicationId, string reason)
    {
        try
        {
            var application = await _db.Applications
                .Include(a => a.Client).ThenInclude(c => c.PersonData)
                .Include(a => a.Status)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return NotFound("Заявка не найдена");

            var rejectedStatus = await _db.ApplicationStatuses
                .FirstOrDefaultAsync(s => s.Name == "Отклонена");

            if (rejectedStatus == null)
            {
                TempData["ErrorMessage"] = "Нет статуса 'Отклонена'";
                return RedirectToAction(nameof(AssignManagerForm), new { id = applicationId });
            }

            if (application.StatusId == rejectedStatus.Id)
            {
                TempData["WarningMessage"] = "Уже отклонена";
                return RedirectToAction(nameof(ApplicationsList));
            }

            long? currentAdminId = _auth.GetCurrentUserId();
            if (currentAdminId == null || currentAdminId == 0) return Unauthorized();

            string commentText = $"Заявка отклонена. Причина: {reason}";
            application.StatusId = rejectedStatus.Id;
            application.UpdatedByEmployeeId = currentAdminId.Value;
            await _historyService.LogChangeAsync(
                applicationId: application.Id,
                employeeId: currentAdminId.Value,
                newStatusId: rejectedStatus.Id,
                comment: commentText
            );

            await _audit.LogAsync("Reject", "Application", application.Id, $"Заявка отклонена. Причина: {reason}");

            var emailTask = new EmailQueue
            {
                RecipientEmail = application.Client.PersonData.Email,
                Subject = "Статус вашей заявки изменён",
                Body = $@"Здравствуйте, {application.Client.PersonData.FullName}.
Ваша заявка #{application.Id} была отклонена.
Причина: {reason}
С уважением, Команда поддержки",
                CreatedAt = DateTime.UtcNow,
                SendStatus = EmailStatus.Pending.ToString()
            };
            _db.EmailQueues.Add(emailTask);

            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Заявка #{application.Id} отклонена. Письмо в очереди.";
            return RedirectToAction(nameof(ApplicationsList));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка при отклонении заявки: {ex.Message}");
            TempData["ErrorMessage"] = "Произошла ошибка при отклонении заявки.";
            return RedirectToAction(nameof(AssignManagerForm), new { id = applicationId });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var app = await _db.Applications
            .Include(a => a.Client).ThenInclude(c => c.PersonData)
            .Include(a => a.Status)
            .Include(a => a.AssignedManager).ThenInclude(m => m.PersonData)
            .Include(a => a.ApplicationServices).ThenInclude(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null) return NotFound();

        ViewBag.StatusHistory = await _historyService.GetHistoryAsync(id);

        return View("~/Views/Admin/ApplicationAdmin/ApplicationDetails.cshtml", app);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(long id, long newStatusId, string? comment)
    {
        var app = await _db.Applications
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null) return NotFound();

        app.StatusId = newStatusId;
        await _db.SaveChangesAsync();

        var currentEmployeeId = _auth.GetCurrentUserId();

        if (currentEmployeeId.HasValue)
        {
            await _historyService.LogChangeAsync(
                applicationId: id,
                employeeId: currentEmployeeId.Value,
                newStatusId: newStatusId,
                comment: comment
            );
            await _audit.LogAsync(
    "ChangeStatus", "Application", id,
    $"Статус изменён на ID: {newStatusId}. Комментарий: {comment ?? "нет"}"
);

        }

        TempData["SuccessMessage"] = "Статус успешно изменён и записан в историю.";
        return RedirectToAction(nameof(Details), new { id });
    }

}
