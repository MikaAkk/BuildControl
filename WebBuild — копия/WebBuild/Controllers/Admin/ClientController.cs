using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Admin;
using WebBuild.Models.Enities;
using WebBuild.Service;

namespace WebBuild.Controllers.Admin;

public class ClientController : AdminController
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _audit;

    public ClientController(AuthService auth, AppDbContext db, AuditLogService audit) : base(auth, db)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IActionResult> ClientIndex(string? search, bool? hasActiveApplications)
    {
        var query = _db.Clients
            .Include(c => c.PersonData).ThenInclude(p => p.PhoneNumber)
            .Include(c => c.Contragent)
            .Include(c => c.Applications).ThenInclude(a => a.Status)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            var lowerSearch = search.ToLower();
            query = query.Where(c =>
                (c.PersonData.Surname + " " + c.PersonData.Name).ToLower().Contains(lowerSearch) ||
                (c.PersonData.PhoneNumber != null && c.PersonData.PhoneNumber.Phone.Contains(search)));
        }

        if (hasActiveApplications.HasValue && hasActiveApplications.Value)
        {
            var excludedStatusIds = await _db.ApplicationStatuses
                .Where(s => s.Name == "Отклонена" || s.Name == "Выполнена")
                .Select(s => s.Id)
                .ToListAsync();

            if (excludedStatusIds.Any())
            {
                query = query.Where(c => c.Applications.Any(a => !excludedStatusIds.Contains(a.StatusId)));
            }
            else
            {
                query = query.Where(c => c.Applications.Any());
            }
        }

        var clients = await query.OrderByDescending(c => c.Id).ToListAsync();

        ViewBag.SearchTerm = search ?? string.Empty;
        ViewBag.ShowActiveOnly = hasActiveApplications ?? false;

        return View("~/Views/Admin/ClientAdmin/ClientList.cshtml", clients);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var client = await _db.Clients
            .Include(c => c.PersonData).ThenInclude(p => p.PhoneNumber)
            .Include(c => c.Contragent)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null) return NotFound();

        var viewModel = new ClientEditViewModel
        {
            Id = client.Id,
            Surname = client.PersonData.Surname,
            Name = client.PersonData.Name,
            Patronymic = client.PersonData.Patronymic,
            Email = client.PersonData.Email,
            PhoneNumberInput = client.PersonData.PhoneNumber?.Phone ?? "",
            CompanyName = client.Contragent?.Name ?? "",
            CompanyAddress = client.Contragent?.Address ?? ""
        };

        return View("~/Views/Admin/ClientAdmin/ClientEdit.cshtml", viewModel);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClientEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("~/Views/Admin/ClientAdmin/ClientEdit.cshtml", model);
        }

        var client = await _db.Clients
            .Include(c => c.PersonData).ThenInclude(p => p.PhoneNumber)
            .Include(c => c.Contragent)
            .FirstOrDefaultAsync(c => c.Id == model.Id);

        if (client == null)
        {
            TempData["ErrorMessage"] = "Клиент не найден.";
            return RedirectToAction(nameof(ClientIndex));
        }
        if (client.PersonData != null)
        {
            client.PersonData.Surname = model.Surname;
            client.PersonData.Name = model.Name;
            client.PersonData.Patronymic = model.Patronymic;
            client.PersonData.Email = model.Email;
            if (!string.IsNullOrWhiteSpace(model.PhoneNumberInput))
            {
                string cleanPhone = NormalizePhone(model.PhoneNumberInput);

                if (client.PersonData.PhoneNumber == null ||
                    client.PersonData.PhoneNumber.Phone != cleanPhone)
                {
                    var phoneId = await GetOrCreatePhoneId(cleanPhone);
                    if (phoneId > 0)
                    {
                        client.PersonData.PhoneNumberId = phoneId;
                    }
                }
            }
            else
            {
                client.PersonData.PhoneNumberId = 0;
            }
        }

        if (!string.IsNullOrWhiteSpace(model.CompanyName) ||
            !string.IsNullOrWhiteSpace(model.CompanyAddress))
        {
            if (client.Contragent == null)
            {
                client.Contragent = new Contragent();
            }
            client.Contragent.Name = model.CompanyName;
            client.Contragent.Address = model.CompanyAddress;
        }
        else
        {
            if (client.Contragent != null)
            {
                _db.Contragents.Remove(client.Contragent);
                client.Contragent = null;
            }
        }

        try
        {
            await _db.SaveChangesAsync();

            await _audit.LogAsync("Update", "Client", model.Id,
                $"Обновлён клиент: {model.Surname} {model.Name}");

            TempData["SuccessMessage"] = $"Клиент «{model.Surname} {model.Name}» успешно обновлён.";
            return RedirectToAction(nameof(ClientIndex));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ошибка при сохранении: {ex.Message}";
            return View("~/Views/Admin/ClientAdmin/ClientEdit.cshtml", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var client = await _db.Clients
            .Include(c => c.PersonData)
            .Include(c => c.Applications)
                .ThenInclude(a => a.ApplicationServices)
            .Include(c => c.Applications)
                .ThenInclude(a => a.StatusHistory)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            TempData["ErrorMessage"] = "Клиент не найден.";
            return RedirectToAction(nameof(ClientIndex));
        }

        var excludedStatusNames = new[] { "Отклонена", "Выполнена" };
        var excludedStatusIds = await _db.ApplicationStatuses
            .Where(s => excludedStatusNames.Contains(s.Name))
            .Select(s => s.Id)
            .ToListAsync();

        var hasActiveApplications = client.Applications
            .Any(a => !excludedStatusIds.Contains(a.StatusId));

        if (hasActiveApplications)
        {
            var appCount = client.Applications.Count(a => !excludedStatusIds.Contains(a.StatusId));
            TempData["ErrorMessage"] =
                $"Невозможно удалить клиента «{client.PersonData.Surname} {client.PersonData.Name}»: " +
                $"у него есть {appCount} активных заявок. Сначала удалите или переназначьте все активные заявки клиента.";
            return RedirectToAction(nameof(ClientIndex));
        }

        string clientName = $"{client.PersonData.Surname} {client.PersonData.Name}";

        foreach (var app in client.Applications)
        {
            if (app.ApplicationServices != null && app.ApplicationServices.Count > 0)
            {
                _db.ApplicationServices.RemoveRange(app.ApplicationServices);
            }

            if (app.StatusHistory != null && app.StatusHistory.Count > 0)
            {
                _db.ApplicationStatusHistories.RemoveRange(app.StatusHistory);
            }
        }

        _db.Applications.RemoveRange(client.Applications);

        if (client.PersonData != null)
        {
            _db.PersonData.Remove(client.PersonData);
        }

        _db.Clients.Remove(client);

        await _db.SaveChangesAsync();

        await _audit.LogAsync("Delete", "Client", id, $"Удалён клиент: {clientName}");

        TempData["SuccessMessage"] = $"Клиент «{clientName}» успешно удалён.";
        return RedirectToAction(nameof(ClientIndex));
    }


    private async Task<long> GetOrCreatePhoneId(string inputPhone)
    {
        if (string.IsNullOrWhiteSpace(inputPhone))
            return 0;

        string cleanPhone = NormalizePhone(inputPhone);
        var existingPhone = await _db.PhoneNumbers
            .FirstOrDefaultAsync(p => p.Phone == cleanPhone);

        if (existingPhone != null)
            return existingPhone.Id;

        var newPhone = new PhoneNumber
        {
            Phone = cleanPhone,
            Description = "Добавлен при редактировании клиента"
        };
        _db.PhoneNumbers.Add(newPhone);
        await _db.SaveChangesAsync();
        return newPhone.Id;
    }

    private string NormalizePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        return phone.Replace(" ", "").Replace("-", "")
                    .Replace("(", "").Replace(")", "").Trim();
    }
}
