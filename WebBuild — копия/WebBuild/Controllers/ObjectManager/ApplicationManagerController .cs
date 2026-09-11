using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBuild.Models;
using WebBuild.Models.ObjectManager;
using WebBuild.Service;


namespace WebBuild.Controllers.ObjectManager;
public class ApplicationManagerController : ObjectManagerController
{
    private readonly AppDbContext _db;
    public ApplicationManagerController(AppDbContext db, AuthService auth)
        : base(db, auth)
    {
        _db = db;
    }

    public async Task<IActionResult> ObjectManagerApplicationList(
       string search = "",
       long statusId = 0,
       int page = 1,
       int pageSize = 20)
    {
        var currentUserId = _auth.GetCurrentUserId();
        if (!currentUserId.HasValue) return Forbid();

        long managerId = currentUserId.Value;

        try
        {
            var query = _db.Applications
                .Include(a => a.Client).ThenInclude(c => c.PersonData).ThenInclude(pd => pd.PhoneNumber)
                .Include(a => a.Contract).ThenInclude(c => c.Status)
                .Include(a => a.ApplicationServices).ThenInclude(a => a.Service)
                .Include(a => a.Status)
                .Where(a => a.AssignedManagerId == managerId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(a =>
                    (a.Client.PersonData.Surname + " " + a.Client.PersonData.Name).ToLower().Contains(search) ||
                    a.Client.PersonData.PhoneNumber.Phone.Contains(search) ||
                    a.Id.ToString().Contains(search));
            }

            if (statusId > 0)
            {
                query = query.Where(a => a.StatusId == statusId);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var applications = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            if (!applications.Any() && totalItems == 0)
            {
                ViewBag.TotalCount = 0;
                ViewBag.Statuses = await _db.ApplicationStatuses.AsNoTracking().ToListAsync();
                ViewBag.SearchTerm = search;
                ViewBag.SelectedStatusId = statusId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                return View("~/Views/ObjectManager/ObjectManager/ObjectManagerApplicationList.cshtml", new List<ApplicationViewManager>());
            }

            var contractIds = applications
                .Where(a => a.Contract != null)
                .Select(a => a.Contract.Id)
                .ToList();

            var objectsQueryResult = new List<(long ContractId, long ObjectId)>();

            if (contractIds.Any())
            {
                var queryResult = await _db.RealEstateObjects
                    .Where(o => o.ContractId.HasValue && contractIds.Contains(o.ContractId.Value))
                    .Select(o => new { o.ContractId, o.Id })
                    .ToListAsync();

                objectsQueryResult = queryResult.Select(x => (x.ContractId.Value, x.Id)).ToList();
            }

            var model = applications.Select(a =>
            {
                long? objId = null;
                bool hasObject = false;

                if (a.Contract != null)
                {
                    var foundObj = objectsQueryResult.FirstOrDefault(o => o.ContractId == a.Contract.Id);
                    if (foundObj != default)
                    {
                        objId = foundObj.ObjectId;
                        hasObject = true;
                    }
                }

                string phone = a.Client?.PersonData?.PhoneNumber?.Phone ?? "Не указан";
                string statusName = a.Status?.Name ?? "Новая";
                decimal totalPrice = a.ApplicationServices?.Sum(s => s.TotalPrice) ?? 0;

                return new ApplicationViewManager
                {
                    Id = a.Id,
                    CreatedAt = a.CreatedAt,
                    ClientFullName = $"{a.Client?.PersonData?.Surname ?? ""} {a.Client?.PersonData?.Name ?? ""}".Trim(),
                    ClientPhone = phone,
                    StatusName = statusName,
                    ServiceNames = a.ApplicationServices
                        ?.Select(s => s.Service?.Name ?? "")
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList() ?? new List<string>(),
                    TotalPrice = totalPrice,
                    HasContract = a.Contract != null,
                    ContractId = a.Contract?.Id,
                    ContractStatusName = a.Contract?.Status?.Name ?? "Нет договора",
                    ObjectId = objId,
                    HasObject = hasObject
                };
            }).ToList();

            ViewBag.TotalCount = totalItems;
            ViewBag.Statuses = await _db.ApplicationStatuses.AsNoTracking().ToListAsync();
            ViewBag.SearchTerm = search;
            ViewBag.SelectedStatusId = statusId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View("~/Views/ObjectManager/ObjectManager/ObjectManagerApplicationList.cshtml", model);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка в ObjectManagerApplicationList: {ex.Message}");
            return StatusCode(500, "Ошибка загрузки списка заявок");
        }
    }


}

