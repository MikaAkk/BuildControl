using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Admin;
using WebBuild.Models.Enities;


namespace WebBuild.Controllers.Admin;

public class EditCreateController : Controller
{
    private readonly AppDbContext _db;

    public EditCreateController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _db.Employees
            .Include(e => e.EmployeeStat)
            .Include(a => a.PersonData)
            .ThenInclude(a => a.PhoneNumber)
            .Include(a => a.Position)
            .Include(a => a.Role)
            .ToListAsync();

        return View("~/Views/Admin/EmployeeSelect.cshtml", employees);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Positions = _db.Positions.ToList();
        ViewBag.Roles = _db.Roles.ToList();
        ViewBag.PhoneNumber = _db.PhoneNumbers.ToList();
        ViewBag.States = _db.EmployeeStat.ToList();
        return View("~/Views/Admin/CreateEmployee.cshtml");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.PhoneNumber = _db.PhoneNumbers.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View("~/Views/Admin/CreateEmployee.cshtml", model);
        }
        var existingPerson = await _db.PersonData.FirstOrDefaultAsync(p => p.Email == model.Email);
        if (existingPerson != null)
        {
            ModelState.AddModelError("Email", "Пользователь с таким Email уже существует.");
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.PhoneNumber = _db.PhoneNumbers.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View("~/Views/Admin/CreateEmployee.cshtml", model);
        }
        var existingState = await _db.EmployeeStat.FindAsync(model.EmployeeStateId);
        if (existingState == null)
        {
            ModelState.AddModelError("EmployeeStateId", "Выбранное состояние не найдено.");
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.PhoneNumber = _db.PhoneNumbers.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View("~/Views/Admin/CreateEmployee.cshtml", model);
        }
        long phoneNumberId = await GetOrCreatePhoneId(model.PhoneNumberInput);
        var person = new PersonData
        {
            Surname = model.Surname,
            Name = model.Name,
            Patronymic = model.Patronymic,
            PhoneNumberId = phoneNumberId,
            Email = model.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.PersonData.Add(person);

        try
        {
            await _db.SaveChangesAsync(); 
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Ошибка при сохранении данных человека: {ex.Message}");
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.PhoneNumber = _db.PhoneNumbers.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View("~/Views/Admin/CreateEmployee.cshtml", model);
        }
        var employee = new Employee
        {
            PeopleId = person.Id,
            PositionId = model.PositionId,
            RoleId = model.RoleId,
            EmployeeStateId = model.EmployeeStateId
        };
        _db.Employees.Add(employee);
        try
        {
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Сотрудник успешно создан!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ошибка при создании сотрудника: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }


    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var employee = await _db.Employees
            .Include(a => a.PersonData)
            .ThenInclude(a => a.PhoneNumber) 
            .FirstOrDefaultAsync(a => a.Id == id);

        if (employee == null || employee.PersonData == null)
            return NotFound();

        string currentPhone = "";


        if (employee.PersonData.PhoneNumberId != 0 && employee.PersonData.PhoneNumber != null)
        {
            currentPhone = employee.PersonData.PhoneNumber.Phone;
        }
        else if (employee.PersonData.PhoneNumberId != 0)
        {
            var orphanPhone = await _db.PhoneNumbers.FindAsync(employee.PersonData.PhoneNumberId);
            if (orphanPhone != null)
            {
                currentPhone = orphanPhone.Phone;
            }
        }

        var viewModel = new EmployeeCreateEditViewModel
        {
            Id = employee.Id,
            Surname = employee.PersonData.Surname,
            Name = employee.PersonData.Name,
            Patronymic = employee.PersonData.Patronymic,
            Email = employee.PersonData.Email,
            PositionId = employee.PositionId,
            RoleId = employee.RoleId,
            EmployeeStateId = employee.EmployeeStateId,
            PhoneNumberInput = currentPhone 
        };

        ViewBag.Positions = _db.Positions.ToList();
        ViewBag.Roles = _db.Roles.ToList();
        ViewBag.States = _db.EmployeeStat.ToList();

        return View("~/Views/Admin/EditEmployee.cshtml", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmployeeCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View(model);
        }

        var employee = await _db.Employees
            .Include(e => e.PersonData)
            .FirstOrDefaultAsync(e => e.Id == model.Id);

        if (employee == null) return NotFound();
        if (employee.PersonData == null) return BadRequest("У сотрудника нет личных данных");

        var existingState = await _db.EmployeeStat.FindAsync(model.EmployeeStateId);
        if (existingState == null)
        {
            ModelState.AddModelError("EmployeeStateId", "Выбранное состояние не найдено.");
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View(model);
        }
        long phoneId = await GetOrCreatePhoneId(model.PhoneNumberInput);
        employee.PersonData.Surname = model.Surname;
        employee.PersonData.Name = model.Name;
        employee.PersonData.Patronymic = model.Patronymic;
        employee.PersonData.PhoneNumberId = phoneId; 
        employee.PersonData.Email = model.Email;

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            if (model.Password.Length < 6)
            {
                ModelState.AddModelError("Password", "Пароль должен быть не менее 6 символов.");
                ViewBag.Positions = _db.Positions.ToList();
                ViewBag.Roles = _db.Roles.ToList();
                ViewBag.States = _db.EmployeeStat.ToList();
                return View(model);
            }

            employee.PersonData.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        }
        employee.PositionId = model.PositionId;
        employee.RoleId = model.RoleId;
        employee.EmployeeStateId = model.EmployeeStateId;

        try
        {
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Сотрудник успешно обновлен!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Ошибка при сохранении: " + ex.Message;
            ViewBag.Positions = _db.Positions.ToList();
            ViewBag.Roles = _db.Roles.ToList();
            ViewBag.States = _db.EmployeeStat.ToList();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var employee = await _db.Employees
                .IgnoreQueryFilters()
                .Include(e => e.PersonData)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                await transaction.RollbackAsync();
                return NotFound("Сотрудник не найден");
            }

            if (employee.IsDeleted)
            {
                await transaction.RollbackAsync(); 
                TempData["WarningMessage"] = "Этот сотрудник уже был деактивирован ранее.";
                return RedirectToAction(nameof(Index));
            }

            var fallbackManager = await _db.Employees
                .Where(e => e.Id != id && !e.IsDeleted && e.EmployeeStateId == 1)
                .OrderBy(e => e.Id)
                .FirstOrDefaultAsync();

            if (fallbackManager == null)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Нельзя деактивировать последнего активного сотрудника. Оставьте хотя бы одного.";
                return RedirectToAction(nameof(Index));
            }

            long fallbackManagerId = fallbackManager.Id;
            var applicationsToReassign = await _db.Applications
                .Where(a => a.AssignedManagerId == id)
                .ToListAsync();

            int reassignedCount = 0;
            foreach (var app in applicationsToReassign)
            {
                app.AssignedManagerId = fallbackManagerId;
                app.UpdatedByEmployeeId = employee.Id;
                app.UpdatedAt = DateTime.UtcNow;
                reassignedCount++;
            }

            await _db.SaveChangesAsync();
            employee.IsDeleted = true;
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Сотрудник '{employee.PersonData?.FullName}' успешно деактивирован. {reassignedCount} заявок переназначено на менеджера ID {fallbackManagerId}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var rootCause = ex;
            while (rootCause.InnerException != null)
                rootCause = rootCause.InnerException;

            System.Diagnostics.Debug.WriteLine($"DB ERROR (Delete Employee): {rootCause.Message}");

            TempData["ErrorMessage"] = $"Произошла ошибка при деактивации сотрудника: {rootCause.Message}";
            return RedirectToAction(nameof(Index));
        }
    }


    private async Task<long> GetOrCreatePhoneId(string inputPhone)
    {
        if (string.IsNullOrWhiteSpace(inputPhone))
            throw new ArgumentException("Номер телефона обязателен");

        string cleanPhone = NormalizePhone(inputPhone);

        var existingPhone = await _db.PhoneNumbers.FirstOrDefaultAsync(p => p.Phone == cleanPhone);

        if (existingPhone != null)
            return existingPhone.Id;

        var newPhone = new PhoneNumber
        {
            Phone = cleanPhone,
            Description = "Добавлен при создании сотрудника" 
        };

        _db.PhoneNumbers.Add(newPhone);
        await _db.SaveChangesAsync();

        return newPhone.Id;
    }

    private string NormalizePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        return phone
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Trim();
    }

}
