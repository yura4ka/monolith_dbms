using Microsoft.AspNetCore.Mvc;
using monolith_dbms.Models.ViewModels;
using monolith_dbms.Services;

namespace monolith_dbms.Controllers
{
    public class DatabaseController : Controller
	{
		private readonly IConnectionManager _connectionManager;
		private readonly ILogger<DatabaseController> _logger;

		public DatabaseController(IConnectionManager connectionManager, ILogger<DatabaseController> logger)
		{
			_connectionManager = connectionManager;
			_logger = logger;
		}

        [Route("Database/{id}")]
		public IActionResult Index(string id)
		{
			var database = _connectionManager.GetConnectionById(id);
			if (database == null) return NotFound();

            var model = new DatabaseViewModel
            {
                Id = id,
                Name = database.Name,
                Tables = database.Tables,
            };

            return View("Index", model);
		}

		[Route("Database/{id}/{tableName}")]
		public IActionResult Table(string id, string tableName)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var selectedTable = database.Tables.Find(t => t.Name == tableName);
            if (selectedTable == null) return NotFound();

            selectedTable.GetAllRows();

            var model = new DatabaseViewModel
            {
                Id = id,
                Name = database.Name,
                Tables = database.Tables,
                SelectedTable = selectedTable,
            };

            return View("Index", model);
        }

		[Route("Database/{id}/{tableName}/{columnName}")]
		public IActionResult Column(string id, string tableName, string columnName)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

			var selectedTable = database.Tables.Find(t => t.Name == tableName);
			if (selectedTable == null) return NotFound();

            var selectedColumn = selectedTable.Columns.Find(c => c.Name == columnName);
            if (selectedColumn == null) return NotFound();

			var model = new DatabaseViewModel
            {
                Id = id,
                Name = database.Name,
                Tables = database.Tables,
                SelectedTable = selectedTable,
                SelectedColumn = selectedColumn,
            };

            return View("Index", model);
        }

        [HttpPost]
        [Route("Database/DropTable")]
        public IActionResult DropTable(string id, string tableName)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var table = database.Tables.Find(t =>t.Name == tableName);
            if (table == null) return NotFound();

            database.DropTable(table);
            return RedirectToAction("Index", new { id });
        }

        [HttpPost]
        [Route("Database/DeleteRow")]
        public IActionResult DeleteRow(string id, string tableName, string pkValue)
        {
            Console.WriteLine($"Delete row value: {pkValue} {pkValue.GetType().Name}");
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var table = database.Tables.Find(t => t.Name == tableName);
            if (table == null) return NotFound();

            table.DeleteRow(pkValue);
            return RedirectToAction("Table", new { id, tableName });
        }
    }
}
