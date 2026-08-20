using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;

namespace WebBuild.Controllers;

[Route("Calculator")]
public class PublicController : Controller
{
    private readonly AppDbContext _db;
    public PublicController(AppDbContext db)
    {
        _db = db;
    }
    [HttpGet]
    public async Task<IActionResult> Calculator()
    {
        var services = await _db.WorkerServices
            .Where(s => s.IsActive)
            .ToListAsync();

        ViewBag.Services = services;
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitCalculatorRequest(CalculatorViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var services = await _db.WorkerServices.Where(s => s.IsActive).ToListAsync();
            ViewBag.Services = services;
            return View("Calculator", model);
        }
        try
        {
            string cleanPhone = model.ClientPhone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            var parts = model.ClientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string surname = parts.Length > 0 ? parts[0] : "Клиент";
            string name = parts.Length > 1 ? parts[1] : "Неизвестно";
            string inputEmail = !string.IsNullOrWhiteSpace(model.EmailAddress)
                ? model.EmailAddress
                : $"{surname}_{name}@gmail.com";
            Contragent contragentEntity = null;
            if (!string.IsNullOrWhiteSpace(model.CompanyName))
            {
                var existingContragent = await _db.Contragents
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == model.CompanyName.ToLower());
                if (existingContragent != null)
                {
                    contragentEntity = existingContragent;
                }
                else
                {
                    contragentEntity = new Contragent
                    {
                        Name = model.CompanyName,
                        Address = "Адрес не указан"
                    };
                    _db.Contragents.Add(contragentEntity);
                }
            }
            PhoneNumber phoneEntity;
            var existingPhone = await _db.PhoneNumbers.FirstOrDefaultAsync(p => p.Phone == cleanPhone);
            if (existingPhone != null)
            {
                phoneEntity = existingPhone;
            }
            else
            {
                phoneEntity = new PhoneNumber
                {
                    Phone = cleanPhone,
                    Description = "Из калькулятора"
                };
                _db.PhoneNumbers.Add(phoneEntity);
                await _db.SaveChangesAsync();
            }
            long realPhoneId = phoneEntity.Id;
            var existingPerson = await _db.PersonData.FirstOrDefaultAsync(p =>
                p.Surname.ToLower() == surname.ToLower() &&
                p.Name.ToLower() == name.ToLower());
            PersonData personEntity;
            if (existingPerson != null)
            {
                personEntity = existingPerson;
                if (personEntity.Email != inputEmail) personEntity.Email = inputEmail;
                if (personEntity.PhoneNumberId != realPhoneId)
                {
                    personEntity.PhoneNumberId = realPhoneId;
                    personEntity.PhoneNumber = null; 
                }
            }
            else
            {
                personEntity = new PersonData
                {
                    Surname = surname,
                    Name = name,
                    Email = inputEmail,
                    PasswordHash = string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    PhoneNumberId = realPhoneId
                };
                _db.PersonData.Add(personEntity);
            }
            var existingClient = await _db.Clients.FirstOrDefaultAsync(c => c.PersonDataId == personEntity.Id);
            Client clientEntity;
            if (existingClient != null)
            {
                clientEntity = existingClient;
                if (contragentEntity != null && clientEntity.ContragentId != contragentEntity.Id)
                {
                    clientEntity.Contragent = contragentEntity;
                }
            }
            else
            {
                clientEntity = new Client
                {
                    PersonData = personEntity,
                    Contragent = contragentEntity
                };
                _db.Clients.Add(clientEntity);
            }
            long managerId = 1;
            long statusId = 1;
            var service = await _db.WorkerServices.FindAsync(model.ServiceId);
            if (service == null) return BadRequest("Услуга не найдена");
            decimal rawQuantity = model.Quantity ?? 0m;
            decimal finalQuantity = rawQuantity <= 0 ? 1m : rawQuantity;
            decimal totalPrice = service.BasePrice * finalQuantity;
            var application = new Application
            {
                Client = clientEntity, 
                StatusId = statusId,
                AssignedManagerId = managerId,
                CreatedByEmployeeId = managerId,
                UpdatedByEmployeeId = managerId,
                AdminComment = $"Онлайн-заявка. Услуга: {service.Name}. Комментарий: {model.Comment}",
                CreatedAt = DateTime.UtcNow
            };

            _db.Applications.Add(application);
            await _db.SaveChangesAsync();
            Console.WriteLine($"Заявка создана! ID: {application.Id}");

            var appService = new ApplicationService
            {
                ApplicationId = application.Id,
                ServiceId = service.Id,
                Quantity = finalQuantity,
                PricePerUnit = service.BasePrice,
                TotalPrice = totalPrice
            };
            _db.ApplicationServices.Add(appService);

            var history = new ApplicationStatusHistory
            {
                ApplicationId = application.Id,
                StatusId = statusId,
                ChangedByEmployeeId = managerId,
                ChangeComment = "Создано через калькулятор",
                ChangedAt = DateTime.UtcNow
            };
            _db.ApplicationStatusHistories.Add(history);
            await _db.SaveChangesAsync();
            TempData["SuccessMessage"] = "Заявка успешно отправлена!";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ОШИБКА: " + ex.Message);
            if (ex.InnerException != null)
            {
                Console.WriteLine("Детали: " + ex.InnerException.Message);
                if (ex.InnerException is Npgsql.PostgresException pgEx)
                {
                    Console.WriteLine($"Postgres Code: {pgEx.SqlState}, Message: {pgEx.Message}");
                }
            }
            TempData["Error"] = $"Ошибка: {ex.Message}";
            return RedirectToAction(nameof(Calculator));
        }
    }
    public IActionResult Success() => View();
}


