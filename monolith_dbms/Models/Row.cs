using System.Collections;

namespace monolith_dbms.Models
{
	public class Row : IEnumerable<ColumnValue>
	{
		private readonly ColumnValue[] _data;

		public Row(ColumnValue[] data)
		{
			_data = data;
		}

		public ColumnValue this[int position] => _data[position];

		public IEnumerator<ColumnValue> GetEnumerator()
		{
			return ((IEnumerable<ColumnValue>)_data).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _data.GetEnumerator();
		}
	}
}
