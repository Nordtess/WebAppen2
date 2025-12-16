using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class MyCvController : Controller
    {
        public IActionResult Index()
        {
            return View("MyCV");
        }
    }
}
