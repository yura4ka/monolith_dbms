namespace monolith_dbms.Models
{
	public class Column
	{
		private string _name;
		private IColumnType _type;
		private bool _isNotNull;
		private object? _defaultValue;
		private bool _isPk;

		public Column(string name, IColumnType type, bool isNotNull = false, object? defaultValue = null, bool isPk = false)
		{
			_name = name;
			_type = type;
			_isNotNull = isNotNull;
			_defaultValue = defaultValue;
			_isPk = isPk;
		}

		public string Name => _name;
		public IColumnType Type => _type;
		public string TypeName => _type.Name;
		public bool IsNotNull => _isNotNull;
		public object? DefaultValue => _defaultValue;
		public bool IsPk => _isPk;
	}
}
