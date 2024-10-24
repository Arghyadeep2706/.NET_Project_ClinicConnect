using ClinicConnect.Models;
using ClinicConnect.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicConnect.Controllers
{
    public class PharmacistController : Controller
    {
        ClinicConnectContext dc=new ClinicConnectContext();
        public IActionResult HomePharmacist()
        {
            return View();
        }

   
        [HttpGet]
        public IActionResult AddMedicine()
        {
            ViewBag.Medicines = dc.Medicines.ToList();
            return View();
        }

        [HttpPost]
        public IActionResult AddMedicine(Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                medicine.PharmMemberId = 1; 
                dc.Medicines.Add(medicine);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        return RedirectToAction("ShowAllMedicine");
                    }
                    else
                    {
                        ViewData["Message"] = "Failed to add medicine.";
                    }
                }
                catch (Exception ex)
                {
                   
                    ViewData["Message"] = $"An error occurred while adding the medicine: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Model state is invalid.";
            }

            ViewBag.Medicines = dc.Medicines.ToList();
            return View(medicine);
        }



        public IActionResult ShowAllMedicine()
        {
            var medicines = dc.Medicines.ToList();
            return View(medicines);
        }

        [HttpGet]
        public IActionResult EditMedicine(int id)
        {
            var medicine = dc.Medicines.Find(id);
            if (medicine == null)
            {
                return NotFound();
            }
            return View(medicine);
        }

        [HttpPost]
        public IActionResult EditMedicine(Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                var existingMedicine = dc.Medicines.Find(medicine.MedId);
                if (existingMedicine != null)
                {
                    existingMedicine.MedName = medicine.MedName;
                    existingMedicine.MedBrand = medicine.MedBrand;
                    existingMedicine.MedAvailability = medicine.MedAvailability;
                    existingMedicine.MedPrice = medicine.MedPrice;

                    try
                    {
                        int result = dc.SaveChanges();
                        if (result > 0)
                        {
                            return RedirectToAction("ShowAllMedicine");
                        }
                        else
                        {
                            ViewData["Message"] = "Failed to update medicine.";
                        }
                    }
                    catch (Exception ex)
                    {
                       
                        ViewData["Message"] = $"An error occurred while updating the medicine: {ex.Message}";
                    }
                }
                else
                {
                    ViewData["Message"] = "Medicine not found.";
                }
            }
            else
            {
                ViewData["Message"] = "Model state is invalid.";
            }
            return View(medicine);
        }

       
       
        [HttpGet]
        public IActionResult DeleteMedicine(int id)
        {
            var medicine = dc.Medicines.Find(id);
            if (medicine == null)
            {
                return NotFound();
            }
            return View(medicine);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var medicine = dc.Medicines.Find(id);
            if (medicine != null)
            {
                dc.Medicines.Remove(medicine);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        return RedirectToAction("ShowAllMedicine");
                    }
                    else
                    {
                        ViewData["Message"] = "Failed to delete medicine.";
                    }
                }
                catch (Exception ex)
                {
                 
                    ViewData["Message"] = $"An error occurred while deleting the medicine: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Medicine not found.";
            }
            return View(medicine);
        }





        public IActionResult Create()
        {
            return View(new BillingViewModel());
        }

        [HttpPost]
        public IActionResult Create(BillingViewModel billingViewModel)
        {
            if (ModelState.IsValid)
            {
            
                var totalPrice = billingViewModel.BillingMedicines.Sum(m => m.Quantity * m.Price);

          
                var discountedPrice = totalPrice - billingViewModel.Discount;
                discountedPrice = discountedPrice < 0 ? 0 : discountedPrice;

             
                var finalPrice = discountedPrice + billingViewModel.Tax;

              
                var billing = new Billing
                {
                    PatientId = billingViewModel.PatientId,
                    Tax = billingViewModel.Tax,
                    Discount = billingViewModel.Discount,
                    TotalPrice = finalPrice
                };

            
                dc.Billings.Add(billing);
                dc.SaveChanges();

            
                foreach (var medicine in billingViewModel.BillingMedicines)
                {
                    var medicineEntity = dc.Medicines
                        .FirstOrDefault(m => m.MedName == medicine.MedName);

                    if (medicineEntity != null)
                    {
                        var billingMedicine = new BillingMedicine
                        {
                            MedId = medicineEntity.MedId,
                            Quantity = medicine.Quantity,
                            BillingId = billing.BillingId
                        };

                        dc.BillingMedicines.Add(billingMedicine);
                    }
                }

                dc.SaveChanges();

            
                return RedirectToAction("BillDetails", new { id = billing.BillingId });
            }

            return View(billingViewModel);
        }




        public IActionResult BillDetails(int id)
        {
            var billing = dc.Billings
                .FirstOrDefault(b => b.BillingId == id);

            if (billing == null)
            {
                return NotFound();
            }

            return View(billing.TotalPrice); 
        }
    }
}
