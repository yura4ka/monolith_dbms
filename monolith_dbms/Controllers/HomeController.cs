using Microsoft.AspNetCore.Mvc;
using monolith_dbms.Models;
using monolith_dbms.Models.ViewModels;
using monolith_dbms.Services;
using System.Diagnostics;

namespace monolith_dbms.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConnectionManager _connectionManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IConnectionManager connectionManager, ILogger<HomeController> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var files = GetDatabases();
            return View(files);
        }

        [HttpPost]
        public IActionResult Connect(string fullPath)
        {
            string id = _connectionManager.CreateConnection(fullPath);
            return RedirectToAction("Index", "Database", new { id });
        }

        [HttpPost]
        public IActionResult Delete(string fullPath)
        {
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(NewDbModel newDb)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\dbs", newDb.Name ?? "");
            if (!fullPath.EndsWith(".db")) fullPath += ".db";
            if (Path.Exists(fullPath))
            {
                ModelState.AddModelError("Name", "Already exists");
            }

            if (ModelState.IsValid)
            {
                string id = _connectionManager.CreateConnection(fullPath);
                return RedirectToAction("Index", "Database", new { id });
            }

            return View(newDb);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private IEnumerable<DbFileModel> GetDatabases()
        {
            return Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\dbs"), "*.db")
                .Select(f => new DbFileModel { Name = Path.GetFileName(f), FullPath = f })
                .ToList();
        }
    }
}
