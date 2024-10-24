using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ClinicConnect.ViewModels
{
    public class BillingViewModel
    {
        [Required]
        public int PatientId { get; set; }

        public List<BillingMedicineViewModel> BillingMedicines { get; set; } = new List<BillingMedicineViewModel>();

        [Range(0.0, double.MaxValue, ErrorMessage = "Discount must be a non-negative value.")]
        public decimal Discount { get; set; } 

        [Range(0.0, double.MaxValue, ErrorMessage = "Tax must be a non-negative value.")]
        public decimal Tax { get; set; }
    }

    public class BillingMedicineViewModel
    {
        [Required]
        public string MedBrand { get; set; }
        [Required]
        public string MedName { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
