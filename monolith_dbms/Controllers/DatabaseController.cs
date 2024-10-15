using Microsoft.AspNetCore.Mvc;
using monolith_dbms.Models;
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
        public IActionResult Table(string id, string tableName, string? search)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var selectedTable = database.Tables.Find(t => t.Name == tableName);
            if (selectedTable == null) return NotFound();

            selectedTable.GetAllRows(search);

            var model = new DatabaseViewModel
            {
                Id = id,
                Name = database.Name,
                Tables = database.Tables,
                SelectedTable = selectedTable,
                SearchString = search,
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

        [HttpGet]
		[Route("Database/{id}/CreateRow/{tableName}")]
		public IActionResult CreateRow(string id, string tableName)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var selectedTable = database.Tables.Find(t => t.Name == tableName);
            if (selectedTable == null) return NotFound();

            var values = new Dictionary<string, string>();
			var columnNames = new Dictionary<string, string>();
			foreach (var col in selectedTable.Columns)
            {
                values.Add(col.Name, col.DefaultValue?.ToString() ?? "");
				string columnName = $"{col.Name} ({col.TypeName})";
				if (col.IsPk) columnName += " (pk)";
				if (col.IsNotNull) columnName += " (nn)";
                columnNames.Add(col.Name, columnName);
			}

            return View(new EditRowModel { Values = values, ColumnNames = columnNames });
        }

        [HttpPost]
		[Route("Database/{id}/CreateRow/{tableName}")]
		public IActionResult CreateRow(string id, string tableName, EditRowModel editRow)
        {
			var database = _connectionManager.GetConnectionById(id);
			if (database == null) return NotFound();

			var selectedTable = database.Tables.Find(t => t.Name == tableName);
			if (selectedTable == null) return NotFound();

			var columnValues = new ColumnValue[selectedTable.Columns.Count];
            foreach (var item in editRow.Values)
            {
                int index = selectedTable.Columns.FindIndex(c => c.Name == item.Key);
                if (index == -1)
                {
					ModelState.AddModelError($"Values[{item.Key}]", $"Error! {item.Key} column doesn't exists");
                    break;
				}

				var c = selectedTable.Columns[index];
				columnValues[index] = c.Type.Instance(null, !c.IsNotNull);
				bool isValid = columnValues[index].ParseString(item.Value);
                if (!isValid) ModelState.AddModelError($"Values[{item.Key}]", $"Wrong value!");
            }

            if (!ModelState.IsValid)
            {
				return View(editRow);
			}

			try
			{
				bool isValid = selectedTable.AddRow(new Row(columnValues));
                if (!isValid) ModelState.AddModelError("", "Error!");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", ex.Message);
			}

            if (!ModelState.IsValid) return View(editRow);
            return RedirectToAction("Table", new { id, tableName });
        }

        [HttpPost]
        [Route("Database/DropTable")]
        public IActionResult DropTable(string id, string tableName)
        {
            var database = _connectionManager.GetConnectionById(id);
            if (database == null) return NotFound();

            var table = database.Tables.Find(t => t.Name == tableName);
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
