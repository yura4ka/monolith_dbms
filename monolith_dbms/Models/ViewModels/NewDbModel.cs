using System.ComponentModel.DataAnnotations;

namespace monolith_dbms.Models.ViewModels
{
    public class NewDbModel
    {
        [Required]
        public string? Name { get; set; }
    }

}
