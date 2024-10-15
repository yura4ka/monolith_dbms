namespace monolith_dbms.Models
{
	public class DbmsException : Exception
	{
		public DbmsException(string msg) : base(msg) { }
	}

	public class PkNotFoundException : DbmsException
	{
		public PkNotFoundException(string tableName) : base($"Error! Cannot find primary key column in '{tableName}' table") { }
	}
}
