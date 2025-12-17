using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class MessagesController : Controller
    {
        public IActionResult Index()
        {
            return View("Messages");
        }
    }
}
