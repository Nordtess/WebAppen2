using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class EditCVController : Controller
    {
        public IActionResult Index()
        {
            return View("EditCV");
        }
    }
}
