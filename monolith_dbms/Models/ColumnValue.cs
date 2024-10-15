using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace monolith_dbms.Models
{
	public abstract class ColumnValue
	{
		protected readonly bool _isNullable;

		public ColumnValue(object? value, bool isNullable)
		{
			_isNullable = isNullable;
			SetFromObject(value);
		}

		public bool IsNullable => _isNullable;

		public abstract string StringValue { get; }
		public abstract object? ObjectValue { get; }
		public abstract bool IsNull { get; }
		public abstract bool SetFromObject(object? value);
		public abstract bool ParseString(string value);
	}

	public class IntValue : ColumnValue
	{
		private int? _value = null;

		public IntValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override bool IsNull => _value == null;

		public override string StringValue => _value?.ToString() ?? "null";

		public override object? ObjectValue => _value;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is string strValue) return ParseString(strValue);
			if (value is int intValue)
			{
				_value = intValue;
				return true;
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}

			bool isValid = int.TryParse(value, out int result);
			if (isValid) _value = result;
			return isValid;
		}
	}

	public class RealValue : ColumnValue
	{
		private double? _value = null;

		public RealValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override bool IsNull => _value == null;

		public override string StringValue => _value?.ToString() ?? "null";

		public override object? ObjectValue => _value;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is string strValue) return ParseString(strValue);
			if (value is double doubleValue)
			{
				_value = doubleValue;
				return true;
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}

			bool isValid = double.TryParse(value, out double result);
			if (isValid) _value = result;
			return isValid;
		}
	}

	public class TextValue : ColumnValue
	{
		private string? _value = null;

		public TextValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override bool IsNull => _value == null;

		public override string StringValue => _value == null ? "null" : $"\"{_value}\"";

		public override object? ObjectValue => _value;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is string strValue)
			{
				_value = strValue;
				return true;
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value.StartsWith('\"') && value.EndsWith('\"'))
			{
				_value = value[1..^1];
				return true;
			}
			_value = value;
			return true;
		}
	}

	public class CharValue : ColumnValue
	{
		private char? _value = null;

		public CharValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override string StringValue => _value?.ToString() ?? "null";

		public override object? ObjectValue => _value;

		public override bool IsNull => _value == null;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is char charValue)
			{
				_value = charValue;
				return true;
			}
			if (value is string str)
			{
				return ParseString(str);
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}

			_value = value[0];
			return true;
		}
	}

	public class ComplexIntValue : ColumnValue
	{
		private const string _regexPattern =
			// Match any int, negative or positive, group it
			@"^([-+]?\d+)" +
			// ... possibly following that with whitespace
			@"\s*" +
			// ... followed by a sign
			@"([-+])" +
			// and possibly more whitespace:
			@"\s*" +
			// Match any other int, and save it
			@"(\d+)" +
			// ... followed by 'i'
			@"i$";

		private static readonly Regex _regex = new Regex(_regexPattern);

		private Complex? _value = null;

		public ComplexIntValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override string StringValue =>  _value == null ? "null" : $"{_value?.Real}{(_value?.Imaginary < 0 ? "" : "+")}{_value?.Imaginary}i";

		public override object? ObjectValue => _value == null ? null : StringValue;

		public override bool IsNull => _value == null;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is string str)
			{
				return ParseString(str);
			}
			if (value is Complex c)
			{
				if (!(double.IsInteger(c.Real) && double.IsInteger(c.Imaginary))) return false;
				_value = c;
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}

			var match = _regex.Match(value);
			if (!match.Success) return false;

			bool resultReal = int.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out int real);
			bool resultImg = int.TryParse($"{match.Groups[2].Value}{match.Groups[3].Value}", CultureInfo.InvariantCulture, out int img);
			if (!(resultReal && resultImg)) return false;

			_value = new Complex(real, img);
			return true;
		}
	}

	public class ComplexRealValue : ColumnValue
	{
		private const string _regexPattern =
			// Match any float, negative or positive, group it
			@"^([-+]?\d+\.?\d*|[-+]?\d*\.?\d+)" +
			// ... possibly following that with whitespace
			@"\s*" +
			// ... followed by a sign
			@"([-+])" +
			// and possibly more whitespace:
			@"\s*" +
			// Match any other float, and save it
			@"(\d+\.?\d*|\d*\.?\d+)" +
			// ... followed by 'i'
			@"i$";

		private static readonly Regex _regex = new Regex(_regexPattern);

		private Complex? _value = null;

		public ComplexRealValue(object? value, bool isNullable) : base(value, isNullable) { }

		public override string StringValue => _value == null ? "null" : $"{_value?.Real}{(_value?.Imaginary < 0 ? "" : "+")}{_value?.Imaginary}i";

		public override object? ObjectValue => _value == null ? null : StringValue;

		public override bool IsNull => _value == null;

		public override bool SetFromObject(object? value)
		{
			if (value == null)
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}
			if (value is string str)
			{
				return ParseString(str);
			}
			if (value is Complex c)
			{
				_value = c;
			}
			return false;
		}

		public override bool ParseString(string value)
		{
			if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.CurrentCultureIgnoreCase))
			{
				if (_isNullable) _value = null;
				return _isNullable;
			}

			var match = _regex.Match(value);
			if (!match.Success) return false;

			bool resultReal = double.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture, out double real);
			bool resultImg = double.TryParse($"{match.Groups[2].Value}{match.Groups[3].Value}", CultureInfo.InvariantCulture, out double img);
			if (!(resultReal && resultImg)) return false;

			_value = new Complex(real, img);
			return true;
		}
	}
}
