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

		[Route("Database/{id}/View/{tableName}")]
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

		[Route("Database/{id}/View/{tableName}/{columnName}")]
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

		[HttpGet]
		[Route("Database/{id}/EditRow/{tableName}/{pk}")]
		public IActionResult EditRow(string id, string tableName, string pk)
		{
			var database = _connectionManager.GetConnectionById(id);
			if (database == null) return NotFound();

			var selectedTable = database.Tables.Find(t => t.Name == tableName);
			if (selectedTable == null) return NotFound();

			int pkIndex = selectedTable.GetPkColumnIndex();
			if (pkIndex == -1 || string.IsNullOrEmpty(pk)) return NotFound();

			var pkValueObject = selectedTable.Columns[pkIndex].Type.Instance(pk, false);
			var row = selectedTable.Rows.Find(r => pkValueObject.ObjectValue?.Equals(r[pkIndex]?.ObjectValue) ?? false);
			if (row == null) return NotFound();

			var values = new Dictionary<string, string>();
			var columnNames = new Dictionary<string, string>();
			for (int i = 0; i < selectedTable.Columns.Count; i++)
			{
				var col = selectedTable.Columns[i];
				values.Add(col.Name, row[i].StringValue);
				string columnName = $"{col.Name} ({col.TypeName})";
				if (col.IsPk) columnName += " (pk)";
				if (col.IsNotNull) columnName += " (nn)";
				columnNames.Add(col.Name, columnName);
			}

			return View(new EditRowModel { Values = values, ColumnNames = columnNames });
		}

		[HttpPost]
		[Route("Database/{id}/EditRow/{tableName}/{pk}")]
		public IActionResult EditRow(string id, string tableName, string pk, EditRowModel editRow)
		{
			Console.WriteLine("1");
			var database = _connectionManager.GetConnectionById(id);
			if (database == null) return NotFound();

			var selectedTable = database.Tables.Find(t => t.Name == tableName);
			if (selectedTable == null) return NotFound();

			int pkIndex = selectedTable.GetPkColumnIndex();
			if (pkIndex == -1 || string.IsNullOrEmpty(pk)) return NotFound();

			var pkValueObject = selectedTable.Columns[pkIndex].Type.Instance(pk, false);
			var rowIndex = selectedTable.Rows.FindIndex(r => pkValueObject.ObjectValue?.Equals(r[pkIndex]?.ObjectValue) ?? false);
			if (rowIndex == -1) return NotFound();

			bool onlyCheck = false;
			foreach (var item in editRow.Values)
			{
				int index = selectedTable.Columns.FindIndex(c => c.Name == item.Key);
				if (index == -1)
				{
					ModelState.AddModelError($"Values[{item.Key}]", $"Error! {item.Key} column doesn't exists");
					break;
				}

				try
				{
					bool isValid = selectedTable.ChangeCell(rowIndex, index, item.Value, onlyCheck);
					if (!isValid)
					{ 
						ModelState.AddModelError($"Values[{item.Key}]", $"Wrong value!");
						onlyCheck = true;
					}
				}
				catch (Exception ex)
				{
					ModelState.AddModelError("", ex.Message);
				}

			}

            Console.WriteLine($"2 {ModelState.IsValid}");
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
			var database = _connectionManager.GetConnectionById(id);
			if (database == null) return NotFound();

			var table = database.Tables.Find(t => t.Name == tableName);
			if (table == null) return NotFound();

			table.DeleteRow(pkValue);
			return RedirectToAction("Table", new { id, tableName });
		}
	}
}
