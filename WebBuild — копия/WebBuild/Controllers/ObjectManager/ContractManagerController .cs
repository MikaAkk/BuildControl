using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.Enities;
using WebBuild.Models.ObjectManager;
using WebBuild.Service;

namespace WebBuild.Controllers.ObjectManager;

public class ContractManagerController : ObjectManagerController
{
    private readonly AppDbContext _db;
    public ContractManagerController(AppDbContext db, AuthService auth)
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
    public async Task<IActionResult> ContractList()
    {
        long? currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return RedirectToAction("Login", "Auth");

        var managerId = await _db.Employees
            .Where(e => e.PeopleId == currentUserId.Value && !e.IsDeleted)
            .Select(e => e.Id)
            .FirstOrDefaultAsync();

        if (managerId == 0) return View("Error", new { Message = "Не найдена учетная запись сотрудника" });

        var objectIds = await _db.RealEstateObjects
            .Where(o => o.ManagerEmployeeId == managerId)
            .Select(o => o.Id)
            .ToListAsync();

        var contracts = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Template)
            .Include(c => c.Status)
            .Where(c => objectIds.Contains(c.Id)) 
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View("~/Views/ObjectManager/ContractManager/ContractList.cshtml", contracts);
    }

    [HttpGet]
    public async Task<IActionResult> CreateContractFromApp(long applicationId)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var app = await _db.Applications
            .Include(a => a.Client).ThenInclude(c => c.PersonData)
            .Include(a => a.ApplicationServices).ThenInclude(asv => asv.Service)
            .Include(a => a.Contract)
            .FirstOrDefaultAsync(a =>
                a.Id == applicationId &&
                a.AssignedManagerId == currentEmployeeId.Value);

        if (app == null)
        {
            TempData["Error"] = "Заявка не найдена или у вас нет прав на неё.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        if (app.Contract != null)
        {
            TempData["Warning"] = "Договор по этой заявке уже существует.";
            return RedirectToAction(nameof(EditContract), new { id = app.Contract.Id });
        }

        var vm = new ContractCreationViewModel
        {
            ApplicationId = app.Id,
            ClientId = app.Client.Id,
            ClientName = $"{app.Client.PersonData.Surname} {app.Client.PersonData.Name}",
            StartDate = DateTime.UtcNow,                  
            EndDate = DateTime.UtcNow.AddDays(30),        
            Services = app.ApplicationServices.Select(s => new ServiceLineItem
            {
                ServiceId = s.Service.Id,
                ServiceName = s.Service.Name,
                Quantity = s.Quantity,
                Price = s.PricePerUnit,
                Total = s.TotalPrice
            }).ToList()
        };

        await EnsureTemplatesInViewBag();
        return View("~/Views/ObjectManager/ContractManager/CreateContractFromApp.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateContractFromApp(ContractCreationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await EnsureTemplatesInViewBag();
            return View("~/Views/ObjectManager/ContractManager/CreateContractFromApp.cshtml", model);
        }

        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue) return RedirectToAction("Login", "Auth");

        var app = await _db.Applications
            .Include(a => a.Contract)
            .FirstOrDefaultAsync(a =>
                a.Id == model.ApplicationId &&
                a.AssignedManagerId == currentEmployeeId.Value);

        if (app == null || app.Contract != null)
        {
            TempData["Error"] = "Неверная заявка или договор уже создан.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        var draftStatus = await _db.ContractStatuses
            .FirstOrDefaultAsync(s => s.Name == "Черновик")
            ?? await _db.ContractStatuses.FirstOrDefaultAsync();

        if (draftStatus == null)
            return StatusCode(500, "В базе нет статусов договоров!");

        DateTime utcStart = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
        DateTime utcEnd = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);

        var contract = new Contract
        {
            ClientId = model.ClientId,
            TemplateId = model.SelectedTemplateId,
            StatusId = draftStatus.Id,
            CreatedByEmployeeId = currentEmployeeId.Value,
            StartDate = utcStart,    
            EndDate = utcEnd,        
            FilePath = null,
            TerminationReason = null
        };

        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        app.ContractId = contract.Id;
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Договор №{contract.Id} успешно создан.";
        return RedirectToAction(nameof(EditContract), new { id = contract.Id });
    }

    private async Task EnsureTemplatesInViewBag()
    {
        var templates = await _db.ContractTemplates.AsNoTracking().ToListAsync();
        ViewBag.Templates = templates.Select(t => new SelectListItem
        {
            Value = t.Id.ToString(),
            Text = $"{t.Name} (v{t.Version})"
        }).ToList();
    }

    [HttpGet]
    public async Task<IActionResult> EditContract(long id)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Template)
            .Include(c => c.Status)
            .Include(c => c.Signer).ThenInclude(e => e.PersonData)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (c.CreatedByEmployeeId == currentEmployeeId ||
                 c.SignedByEmployeeId == currentEmployeeId));

        if (contract == null)
        {
            TempData["Error"] = "Доступ запрещен.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        var allStatuses = await _db.ContractStatuses
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = s.Id == contract.StatusId
            }).ToListAsync();

        var vm = new ContractEditViewModel
        {
            Id = contract.Id,
            ClientName = $"{contract.Client.PersonData.Surname} {contract.Client.PersonData.Name}",
            ClientEmail = contract.Client.PersonData.Email,
            TemplateName = contract.Template.Name,
            StatusName = contract.Status.Name,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate ?? DateTime.UtcNow.AddDays(30),
            IsSigned = contract.SignedDate.HasValue,
            SignerName = contract.Signer != null
               ? $"{contract.Signer.PersonData.Surname} {contract.Signer.PersonData.Name}"
               : "Не подписан",
            ShouldSign = false,
            SelectedStatusId = contract.StatusId,
            Statuses = allStatuses
        };

        return View("~/Views/ObjectManager/ContractManager/EditContract.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditContract(ContractEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/ObjectManager/ContractManager/EditContract.cshtml", model);

        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue)
            return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Template)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c => c.Id == model.Id);

        if (contract == null)
            return NotFound();

        if (contract.CreatedByEmployeeId != currentEmployeeId)
        {
            TempData["Error"] = "Вы не можете редактировать этот договор.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        contract.StartDate = DateTime.SpecifyKind(model.StartDate, DateTimeKind.Utc);
        contract.EndDate = DateTime.SpecifyKind(model.EndDate, DateTimeKind.Utc);

        var newStatus = await _db.ContractStatuses.FirstOrDefaultAsync(s => s.Id == model.SelectedStatusId);
        if (newStatus != null)
        {
            contract.StatusId = newStatus.Id;

            if (newStatus.Name == "Подписан" && !contract.SignedDate.HasValue)
            {
                contract.SignedDate = DateTime.UtcNow;
                contract.SignedByEmployeeId = currentEmployeeId.GetValueOrDefault();

                var app = await _db.Applications.FirstOrDefaultAsync(a => a.ContractId == contract.Id);
                if (app != null)
                {
                    var appStatus = await _db.ApplicationStatuses.FirstOrDefaultAsync(s => s.Name == "Договор заключен");
                    if (appStatus != null)
                    {
                        app.StatusId = appStatus.Id;
                    }
                }

                TempData["Success"] = "Договор подписан. Теперь можно создать объект.";
            }
        }

        await _db.SaveChangesAsync();

        if (newStatus != null && newStatus.Name == "Подписан")
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");

        TempData["Success"] = "Данные обновлены.";
        return RedirectToAction(nameof(Details), new { id = contract.Id });
    }


    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue) return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Template)
            .Include(c => c.Status)
            .Include(c => c.Signer).ThenInclude(e => e.PersonData)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                (c.CreatedByEmployeeId == currentEmployeeId ||
                 c.SignedByEmployeeId == currentEmployeeId));

        if (contract == null)
        {
            TempData["Error"] = "Договор не найден.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        var app = await _db.Applications
            .Include(a => a.ApplicationServices).ThenInclude(s => s.Service)
            .FirstOrDefaultAsync(a => a.ContractId == contract.Id);

        var vm = new ContractEditViewModel
        {
            Id = contract.Id,
            ClientName = $"{contract.Client.PersonData.Surname} {contract.Client.PersonData.Name}",
            ClientEmail = contract.Client.PersonData.Email,
            TemplateName = contract.Template.Name,
            StatusName = contract.Status.Name,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate ?? DateTime.UtcNow.AddDays(30),  
            IsSigned = contract.SignedDate.HasValue,
            SignerName = contract.Signer != null
             ? $"{contract.Signer.PersonData.Surname} {contract.Signer.PersonData.Name}"
             : "Не подписан",
            ShouldSign = false
        };
        ViewBag.Services = app?.ApplicationServices
            .Select(s => new ServiceLineItem
            {
                ServiceId = s.Service.Id,
                ServiceName = s.Service.Name,
                Quantity = s.Quantity,
                Price = s.PricePerUnit,
                Total = s.TotalPrice
            }).ToList() ?? new List<ServiceLineItem>();

        return View("~/Views/ObjectManager/ContractManager/ContractDetails.cshtml", vm);
    }


    [HttpGet]
    public async Task<IActionResult> EditServices(long id) 
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue) return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Client).ThenInclude(cl => cl.PersonData)
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.CreatedByEmployeeId == currentEmployeeId);

        if (contract == null)
        {
            TempData["Error"] = "Договор не найден.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        var app = await _db.Applications
            .Include(a => a.ApplicationServices).ThenInclude(s => s.Service)
            .FirstOrDefaultAsync(a => a.ContractId == contract.Id);

        if (app == null)
        {
            TempData["Error"] = "Заявка для этого договора не найдена.";
            return RedirectToAction(nameof(EditContract), new { id = contract.Id });
        }

        var allServices = await _db.WorkerServices
            .Where(s => s.IsActive)
            .AsNoTracking()
            .ToListAsync();

        var vm = new ContractServicesViewModel
        {
            ContractId = contract.Id,
            ApplicationId = app.Id,
            ClientName = $"{contract.Client.PersonData.Surname} {contract.Client.PersonData.Name}",
            ContractStatusName = contract.Status?.Name ?? "",
            ExistingServices = app.ApplicationServices.Select(s => new EditableServiceItem
            {
                Id = s.Id,
                ServiceId = s.ServiceId,
                ServiceName = s.Service?.Name ?? "",
                Unit = s.Service?.Unit ?? "",
                Quantity = s.Quantity,
                PricePerUnit = s.PricePerUnit
            }).ToList(),
            AvailableServices = allServices.Select(s => new ServiceOption
            {
                Id = s.Id,
                Name = s.Name,
                Unit = s.Unit ?? "",
                BasePrice = s.BasePrice
            }).ToList()
        };

        return View("~/Views/ObjectManager/ContractManager/EditServices.cshtml", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditServices(ContractServicesViewModel model)
    {
        long? currentEmployeeId = _auth.GetCurrentUserId();
        if (!currentEmployeeId.HasValue) return RedirectToAction("Login", "Auth");

        var contract = await _db.Contracts
            .Include(c => c.Status)
            .FirstOrDefaultAsync(c =>
                c.Id == model.ContractId &&
                c.CreatedByEmployeeId == currentEmployeeId);

        if (contract == null)
        {
            TempData["Error"] = "Договор не найден.";
            return RedirectToAction("ObjectManagerApplicationList", "ApplicationManager");
        }

        if (contract.Status?.Name == "Подписан" || contract.Status?.Name == "Расторгнут")
        {
            TempData["Error"] = "Нельзя изменять услуги подписанного или расторгнутого договора.";
            return RedirectToAction(nameof(EditContract), new { id = contract.Id });
        }

        var app = await _db.Applications
            .Include(a => a.ApplicationServices)
            .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

        if (app == null)
        {
            TempData["Error"] = "Заявка не найдена.";
            return RedirectToAction(nameof(EditContract), new { id = contract.Id });
        }

        System.Diagnostics.Debug.WriteLine($"=== EditServices POST ===");
        System.Diagnostics.Debug.WriteLine($"NewServiceId: {model.NewServiceId}");
        System.Diagnostics.Debug.WriteLine($"NewQuantity: {model.NewQuantity}");
        System.Diagnostics.Debug.WriteLine($"NewPricePerUnit: {model.NewPricePerUnit}");
        System.Diagnostics.Debug.WriteLine($"ExistingServices count: {model.ExistingServices?.Count ?? 0}");

        if (model.ExistingServices != null)
        {
            foreach (var item in model.ExistingServices)
            {
                var dbService = app.ApplicationServices.FirstOrDefault(s => s.Id == item.Id);
                if (dbService == null) continue;

                if (item.ShouldDelete)
                {
                    _db.ApplicationServices.Remove(dbService);
                    continue;
                }

                dbService.Quantity = item.Quantity;
                dbService.PricePerUnit = item.PricePerUnit;
                dbService.TotalPrice = item.Quantity * item.PricePerUnit;
            }
        }

        if (model.NewServiceId.HasValue && model.NewServiceId.Value > 0)
        {
            decimal qty = model.NewQuantity ?? 1;      
            decimal price = model.NewPricePerUnit ?? 0; 
            if (price == 0)
            {
                var svc = await _db.WorkerServices.FindAsync(model.NewServiceId.Value);
                if (svc != null)
                    price = svc.BasePrice;
            }

            var existing = app.ApplicationServices
                .FirstOrDefault(s => s.ServiceId == model.NewServiceId.Value);

            if (existing != null)
            {
                existing.Quantity += qty;
                existing.PricePerUnit = price > 0 ? price : existing.PricePerUnit;
                existing.TotalPrice = existing.Quantity * existing.PricePerUnit;
                System.Diagnostics.Debug.WriteLine($"Updated existing: Id={existing.Id}, Qty={existing.Quantity}");
            }
            else
            {
                var newService = new ApplicationService
                {
                    ApplicationId = app.Id,
                    ServiceId = model.NewServiceId.Value,
                    Quantity = qty,
                    PricePerUnit = price,
                    TotalPrice = qty * price
                };
                app.ApplicationServices.Add(newService);
                System.Diagnostics.Debug.WriteLine($"Added new service: ServiceId={model.NewServiceId.Value}, Qty={qty}, Price={price}");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("NewServiceId is null or 0 — service NOT added");
        }

        contract.UpdatedByEmployeeId = currentEmployeeId;
        contract.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
            TempData["Success"] = "Услуги договора обновлены.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SAVE ERROR: {ex.Message}");
            TempData["Error"] = $"Ошибка: {ex.Message}";
        }

        return RedirectToAction(nameof(EditServices), new { id = contract.Id });
    }


}
