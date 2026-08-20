using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebBuild.Service;

namespace WebBuild.Controllers;
public class EmployeeController : BaseController
{
    public EmployeeController(AuthService auth) : base(auth) { }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();
        if (!roles.Contains("Сотрудник")) 
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }

    public IActionResult Index()
    {
        ViewBag.Title = "Панель руководителя";
        return View();
    }

    public IActionResult Employee()
    {
        return View(); 
    }
}