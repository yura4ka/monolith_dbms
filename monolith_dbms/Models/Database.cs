using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;

namespace monolith_dbms.Models
{
	public interface IDatabase : IDisposable
	{
		public List<Table> Tables { get; }
		public ITableController TableController { get; }
		public string Name { get; }
		public bool CreateTable(Table table);
		public bool DropTable(Table table);
	}

	public class SqliteDatabase : IDatabase
	{
		private readonly SqliteConnection _connection;
		private readonly ITableController _tableController;
		private readonly List<Table> _tables;

		public List<Table> Tables => _tables;

		public ITableController TableController => _tableController;

		public string Name { get; private set; }

		public SqliteDatabase(string name)
		{
			_tables = [];
			_connection = new SqliteConnection($"Data Source={name}");
			_tableController = new SqliteTableController(_connection);
			_connection.Open();
			InitDatabase();
			Name = Path.GetFileName(name);
		}

		private void InitDatabase()
		{
			var getTablesCommand = _connection.CreateCommand();
			getTablesCommand.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";

			using var tableReader = getTablesCommand.ExecuteReader();
			while (tableReader.Read())
			{
				string tableName = tableReader.GetString(0);
				var table = new Table(_tableController, tableName);

				var getColumnInfoCommand = _connection.CreateCommand();
				getColumnInfoCommand.CommandText = $"PRAGMA table_info({tableName});";

				using var columnReader = getColumnInfoCommand.ExecuteReader();
				while (columnReader.Read())
				{
					string columnName = columnReader.GetString(1);
					string columnType = columnReader.GetString(2);
					bool notNull = columnReader.GetBoolean(3);
					object? defaultValue = columnReader.IsDBNull(4) ? null : columnReader.GetValue(4);
					bool isPrimaryKey = columnReader.GetBoolean(5);

					var column = new Column(columnName, TypeManager.TypeMappings[columnType.ToUpper()](), notNull, defaultValue, isPrimaryKey);
					table.AddColumn(column);
				}
				AddTable(table);
			}
		}

		public bool CreateTable(Table table)
		{
			ValidateSqlIdentifier(table.Name);

			var (isValid, message) = table.ValidateColumns();
			if (!isValid) throw new DbmsException(message);

			var columns = table.Columns.Select((c) =>
			{
				ValidateSqlIdentifier(c.Name);
				var columnDef = $"{c.Name} {c.TypeName}";
				if (c.IsPk) columnDef += " PRIMARY KEY";
				if (c.IsNotNull) columnDef += " NOT NULL";
				if (c.DefaultValue != null) columnDef += $" DEFAULT {c.DefaultValue.ToString()}";

				return columnDef;
			});

			var command = _connection.CreateCommand();
			command.CommandText = $"CREATE TABLE {table.Name} ({string.Join(',', columns)});";

			command.ExecuteNonQuery();
			_tables.Add(table);
			return true;
		}

		public bool DropTable(Table table)
		{
			var command = _connection.CreateCommand();
			command.CommandText = $"DROP TABLE {table.Name}";
			command.ExecuteNonQuery();
			_tables.Remove(table);
			return true;
		}

		private void AddTable(Table table)
		{
			_tables.Add(table);
		}

		private void ValidateSqlIdentifier(string identifier)
		{
			if (string.IsNullOrWhiteSpace(identifier) || !Regex.IsMatch(identifier, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
			{
				throw new DbmsException($"Invalid SQL identifier: {identifier}");
			}
		}

		public void Dispose()
		{
			_connection.Dispose();
		}
	}
}
