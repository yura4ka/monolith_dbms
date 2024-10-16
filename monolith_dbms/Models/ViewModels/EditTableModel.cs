using System.ComponentModel.DataAnnotations;

namespace monolith_dbms.Models.ViewModels
{
    public class ColumnDefinitionModel
    {
		[DisplayFormat(ConvertEmptyStringToNull = false)]
		public string? Name { get; set; } = "";
        public string? TypeName { get; set; } = "";
        public bool IsNotNull { get; set; } = false;
        public string? DefaultValue { get; set; } = "";
        public bool? IsDeleted { get; set; } = false;
    }

    public class EditTableModel
    {
        public required string Id { get; set; }
        public string? TableName { get; set; } = "";
        public required List<ColumnDefinitionModel> Columns { get; set; }
        public string[]? AvailableTypes { get; set; } = [];
        public int? PrimaryKey { get; set; } = 0;
    }
}
