namespace monolith_dbms.Models.ViewModels
{
    public class EditRowModel
    {
        public required Dictionary<string, string> Values { get; set; }
		public required Dictionary<string, string> ColumnNames { get; set; }
	}
}
