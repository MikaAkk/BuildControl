using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebBuild.Service;

namespace WebBuild.Controllers.ObjectManage;
public class ObjectManagerController : BaseController
{
    public ObjectManagerController(AuthService auth) : base(auth) { }
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var roles = _auth.GetRoles();
        if (!roles.Contains("Руководитель")) 
        {
            context.Result = new RedirectToActionResult("Index", "Home", null);
        }
    }
    public IActionResult Index()
    {
        ViewBag.Title = "Панель руководителя";
        return View();
    }
    public IActionResult ObjectManager()
    {
        return View(); 
    }
}
