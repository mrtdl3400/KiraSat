using System.ComponentModel.DataAnnotations;

namespace KiraSepet.WebUII.Models
{
    public class BusinessApplicationViewModel
    {
        [Required(ErrorMessage = "İşletme adı zorunludur.")]
        [StringLength(150)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vergi numarası zorunludur.")]
        [StringLength(20)]
        public string TaxNumber { get; set; } = string.Empty;
    }
}
