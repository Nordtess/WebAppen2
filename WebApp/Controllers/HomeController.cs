using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Infrastructure.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var row = await (from p in _db.Projekt.AsNoTracking()
                             join u in _db.Users.AsNoTracking() on p.CreatedByUserId equals u.Id into users
                             from u in users.DefaultIfEmpty()
                             orderby p.CreatedUtc descending
                             select new HomeIndexVm.LatestProjectVm
                             {
                                 Id = p.Id,
                                 Title = p.Titel,
                                 ShortDescription = p.KortBeskrivning,
                                 Description = p.Beskrivning,
                                 CreatedUtc = p.CreatedUtc,
                                 ImagePath = p.ImagePath,
                                 TechKeysCsv = p.TechStackKeysCsv,
                                 CreatedByName = u == null ? null : ((u.FirstName + " " + u.LastName).Trim()),
                                 CreatedByEmail = u == null ? null : u.Email
                             })
                .FirstOrDefaultAsync();

            var vm = new HomeIndexVm
            {
                LatestProject = row
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public sealed class HomeIndexVm
    {
        public LatestProjectVm? LatestProject { get; init; }

        public sealed class LatestProjectVm
        {
            public int Id { get; init; }
            public string Title { get; init; } = string.Empty;
            public string? ShortDescription { get; init; }
            public string? Description { get; init; }
            public DateTimeOffset CreatedUtc { get; init; }
            public string? ImagePath { get; init; }
            public string? TechKeysCsv { get; init; }
            public string? CreatedByName { get; init; }
            public string? CreatedByEmail { get; init; }
        }
    }
}
