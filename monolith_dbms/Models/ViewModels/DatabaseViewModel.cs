namespace monolith_dbms.Models.ViewModels
{
	public class DatabaseViewModel
	{
		public required string Id { get; set; }
		public required string Name { get; set; }
		public required List<Table> Tables { get; set; }
		public Table? SelectedTable { get; set; }
		public Column? SelectedColumn { get; set; }
		public string? SearchString { get; set; }
	}
}
