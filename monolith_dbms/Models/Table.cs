using System.Text.RegularExpressions;

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

		public void GetAllRows(string? search)
		{
			var rows = _tableController.GetAllRows(this);
			if (string.IsNullOrEmpty(search))
			{
				_rows = rows;
				return;
			}

            Func<string, bool> searchFunc;
            try
            {
                var regex = new Regex(search);
                searchFunc = (value) => regex.IsMatch(value);
            }
            catch
            {
                string searchValue = search.ToLower();
                searchFunc = (value) => value.Trim().ToLower().Contains(searchValue);
            }

			_rows = rows.Where(r => r.Any(value => searchFunc(value.StringValue))).ToList();
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

		public void DeleteRow(object pkValue)
		{
			int pkIndex = GetPkColumnIndex();
			if (pkIndex == -1) return;

			var pkValueObject = _columns[pkIndex].Type.Instance(pkValue, false);

			_tableController.DeleteRow(this, pkValueObject.ObjectValue!);
			_rows.RemoveAll(r => r[pkIndex].ObjectValue == pkValueObject);
		}

		public int GetPkColumnIndex()
		{
			return _columns.FindIndex(c => c.IsPk);
		}
	}
}
