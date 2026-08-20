using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebBuild.Models;
using WebBuild.Models.ObjectManager;
using WebBuild.Service;

namespace WebBuild.Controllers.ObjectManage;

public class ApplicationManagerController : Controller
{
    private readonly AppDbContext _db;
    private readonly AuthService _auth;

    public ApplicationManagerController(AppDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }
    public async Task<IActionResult> ObjectManagerApplicationList()
    {
        long managerId = _auth.GetCurrentUserId().Value;

        try
        {

            var rawApplications = await _db.Applications
                .Where(a => a.AssignedManagerId == managerId)
                .Include(a => a.Client)                   
                    .ThenInclude(c => c.PersonData)       
                        .ThenInclude(pd => pd.PhoneNumber) 
                .Include(a => a.Status)
                .Include(a => a.ApplicationServices)
                    .ThenInclude(s => s.Service)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var viewModelList = rawApplications.Select(app => new ApplicationViewManager
            {
                Id = app.Id,
                CreatedAt = app.CreatedAt,
                ClientFullName = app.Client?.PersonData != null
                ? $"{app.Client.PersonData.Surname} {app.Client.PersonData.Name}"
                : "Клиент не указан",

                ClientPhone = app.Client?.PersonData?.PhoneNumber?.Phone ?? "Нет телефона",
                StatusName = app.Status?.Name ?? "Без статуса",

                ServiceNames = app.ApplicationServices
                    .Select(s => s.Service?.Name ?? "Услуга без названия")
                    .ToList(),

                TotalPrice = app.ApplicationServices.Sum(s => s.TotalPrice)
            }).ToList();

            ViewBag.TotalCount = viewModelList.Count;
            return View("~/Views/ObjectManager/ObjectManagerApplicationList.cshtml", viewModelList);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка формирования списка заявок: {ex.Message}");
            return StatusCode(500, "Произошла ошибка при загрузке данных. Проверьте логи.");
        }
    }
}

    
