using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebBuild.Controllers;

public class HomeController : Controller
{
    [Route("")]           
    [Route("Home/Index")]  
    [Route("Index")]        
    [Route("Default")]     
    public IActionResult Index() => View();

    [Route("Login")]
    public IActionResult Login() => View("~/Views/Account/Login.cshtml");

} // HomeController
