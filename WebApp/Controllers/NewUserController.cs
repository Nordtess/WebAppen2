using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class NewUserController : Controller
    {
        public IActionResult NewUser()
        {
            return View();
        }
    }
}
