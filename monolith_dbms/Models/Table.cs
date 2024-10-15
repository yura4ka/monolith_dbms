namespace monolith_dbms.Models
{
	public class Table
	{
		private readonly ITableController _tableController;
		private string _name;
		private List<Column> _columns;
		private List<Row> _rows;

		public Table(ITableController tableController, string name)
		{
			_tableController = tableController;
			_name = name;
			_columns = [];
			_rows = [];
		}

		public string Name => _name;
		public List<Column> Columns => _columns;
		public List<Row> Rows => _rows;

		public void AddColumn(Column column)
		{
			_columns.Add(column);
		}

		public (bool, string) ValidateColumns()
		{
			bool isPkFound = false;
			var names = new HashSet<string>(_columns.Count);
			foreach (var column in _columns)
			{
				if (string.IsNullOrEmpty(column.Name))
					return (false, $"Invalid value for column name: \"{column.Name}\"!");
				if (!names.Add(column.Name))
					return (false, $"Column name is not unique: \"{column.Name}\"!");
				if (column.IsPk)
					isPkFound = true;
				if (column.DefaultValue == null)
					continue;
				var defaultValueObject = column.Type.Instance(null, false);
				if (!defaultValueObject.SetFromObject(column.DefaultValue))
					return (false, $"\"{column.Name}\": invalid default value {defaultValueObject.StringValue} for type {column.Type.Name}!");
			}

			if (!isPkFound)
				return (false, $"Table must have a primary key column!");

			return (true, "");
		}

		public void GetAllRows()
		{
			_rows = _tableController.GetAllRows(this);
		}

		public bool ChangeCell(int row, int column, string value)
		{
			bool isValid;
			bool isPk = _columns[column].IsPk;
			var originalValue = _rows[row][column].ObjectValue;

			if (isPk)
			{
				var columnValue = _columns[column].Type.Instance(null, false);
				if (!columnValue.ParseString(value)) return false;
				isValid = _tableController.UpdatePrimaryKey(this, row, column, columnValue.ObjectValue);
				if (isValid) _rows[row][column].ParseString(value);
			}
			else
			{
				isValid = _rows[row][column].ParseString(value);
				if (!isValid) return false;
				isValid = _tableController.UpdateCell(this, row, column);
				if (!isValid) _rows[row][column].SetFromObject(originalValue);
			}


			return isValid;
		}

		public bool AddRow(Row row)
		{
			bool isValid = _tableController.InsertRow(this, row);
			if (isValid) _rows.Add(row);
			return isValid;
		}

		public void DeleteRow(int row)
		{
			_tableController.DeleteRow(this, row);
			_rows.RemoveAt(row);
		}
	}
}
