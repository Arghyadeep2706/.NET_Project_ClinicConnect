using ClinicConnect.Models;
using DNTCaptcha.Core.Providers;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
namespace ClinicConnect.Controllers
{
  
    public class AdminController : Controller
    {
       ClinicConnectContext dc=new ClinicConnectContext();
        


        public readonly IDNTCaptchaValidatorService _validatorService;

        public AdminController(IDNTCaptchaValidatorService validatorService)
        {

            _validatorService = validatorService;

        }

        public IActionResult HomeAdmin()
        {
            return View();
        }
        private byte[] HashPassword(string password)
        {
            var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateDNTCaptcha(ErrorMessage = "Please Enter Valid Captcha",
            CaptchaGeneratorLanguage = Language.English,
            CaptchaGeneratorDisplayMode = DisplayMode.ShowDigits)]

        public IActionResult Login(string t1, string t2)
        {

           

            if (ModelState.IsValid)
            {
                if (!_validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.ShowDigits))
                {
                    this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
                }

                var res = (from t in dc.Admins
                           where t.AdminEmail == t1 && t.Password == HashPassword(t2)
                           select t).FirstOrDefault();

                if (res != null)
                {
                  

                    return RedirectToAction("HomeAdmin");
                }
                else
                {
                    ViewData["err"] = "Invalid username and password.";
                }


            }
            return View();
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        [HttpPost]
        public IActionResult ForgotPassword(Admin admin)
        {
            if (ModelState.IsValid)
            {
                var user = dc.Admins.FirstOrDefault(t => t.AdminEmail == admin.AdminEmail);
                if (user != null && user.SecurityAnswer == admin.SecurityAnswer)
                {
                    
                    return RedirectToAction("ResetPassword");
                }
                else
                {
                    ViewData["v"] = "Invalid username or security answer.";
                }
            }
            return View(admin);
        }
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ResetPassword(string newPassword, string conPassword, Admin admin)
        {
            var res = dc.Admins.FirstOrDefault(r => r.AdminEmail == admin.AdminEmail);

            if (res != null)
            {
                if (newPassword == conPassword)
                {
                    
                    res.Password = HashPassword(newPassword);

                    int i = dc.SaveChanges();

                    if (i > 0)
                    {
                        ViewData["x"] = "Password reset successfully.";
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ViewData["x"] = "Failed to reset password.";
                    }
                }
                else
                {
                    ViewData["x"] = "New Password and Confirm Password mismatch.";
                }
            }
            else
            {
                ViewData["x"] = "User not found.";
            }

            return View(admin);
        }




        public IActionResult Logout()
        {
            var login = HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }


        [HttpGet]
        public IActionResult HomePage()
        {
            return View();
        }


        [HttpGet]
        public IActionResult PatientRegister()
        {

            return View();
        }

      
        public IActionResult PatientRegister(Patient patient)
        {
            if (ModelState.IsValid)
            {

                

                patient.Password = HashPassword(patient.UserPassword);

                dc.Patients.Add(patient);
                int i = dc.SaveChanges();

                if (i > 0)
                {
                    ViewData["v"] = "You registered successfully";
                }
                else
                {
                    ViewData["v"] = "Error occurred while creating pharmacist";
                }
            }
            else
            {
                ViewData["v"] = "Validation failed";
            }
            return View("Login");

        }


        public class AgeAndDobValidator : ValidationAttribute
        {
            protected override ValidationResult IsValid(object value, ValidationContext validationContext)
            {
                var patient = (Patient)validationContext.ObjectInstance;
                if (patient.Dob.HasValue && patient.PatientAge.HasValue)
                {
                    var calculatedAge = DateTime.Today.Year - patient.Dob.Value.Year;
                    if (patient.Dob.Value.Date > DateTime.Today.AddYears(-calculatedAge)) calculatedAge--;

                    if (calculatedAge != patient.PatientAge.Value)
                    {
                        return new ValidationResult("PatientAge does not match the age calculated from Dob.");
                    }
                }
                return ValidationResult.Success;
            }
        }

        // ------- PATIENT REGISTRATION --------
        public IActionResult PatientRegistration(Patient p)
        {
            var patients = dc.Patients.ToList();
            return View(patients);

        }

        // ------- EDIT PATIENT --------
        [HttpGet]
        public IActionResult EditPatient(int id)
        {
            var patient = dc.Patients.Find(id);
            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPatient(Patient patient)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(patient.UserPassword))
                {
                    patient.Password = HashPassword(patient.UserPassword);
                }

                dc.Patients.Update(patient);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        ViewData["Message"] = "Patient details have been edited successfully.";

                    }
                    else
                    {
                        ViewData["Message"] = "Failed to update patient.";
                    }
                }
                catch (Exception ex)
                {
                    ViewData["Message"] = $"An error occurred while updating the patient: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Model state is invalid.";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ViewData["Message"] += $" {error.ErrorMessage}";
                }
            }
            return View(patient);
        }


        // ------- DELETE PATIENT --------
        [HttpGet]
        public IActionResult DeletePatient(int id)
        {
            var patient = dc.Patients.Find(id);
            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        [HttpPost, ActionName("DeletePatient")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var patient = dc.Patients.Find(id);
            if (patient != null)
            {
                dc.Patients.Remove(patient);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        ViewData["Message"] = "Patient details have been deleted successfully.";

                    }
                    else
                    {
                        ViewData["Message"] = "Failed to delete patient.";
                    }
                }
                catch (Exception ex)
                {
                    ViewData["Message"] = $"An error occurred while deleting the patient: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Patient not found.";
            }
            return View(patient);
        }
     
        public IActionResult Patient()
        {
            return View();
        }



        [HttpGet]
        public IActionResult DoctorRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateDNTCaptcha(ErrorMessage = "Please Enter Valid Captcha",
            CaptchaGeneratorLanguage = Language.English,
            CaptchaGeneratorDisplayMode = DisplayMode.ShowDigits)]
        public IActionResult DoctorRegister(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                if (!_validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.ShowDigits))
                {
                    this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
                }
                else
                {
                    doctor.Password = HashPassword(doctor.UserPassword);
                    dc.Doctors.Add(doctor);
                    int result = dc.SaveChanges();

                    if (result > 0)
                    {
                        ViewData["v"] = "Doctor Registerated successfully";
                    }
                    else
                    {
                        ViewData["v"] = "Error occurred while creating doctor";
                    }
                }
            }
            else
            {
                ViewData["v"] = "Validation failed";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ViewData["v"] += $" {error.ErrorMessage}";
                }
            }
            return View(doctor);
        }

        public IActionResult DoctorRegistration()
        {
            var doctors = dc.Doctors.ToList();
            return View(doctors);
        }

        // ------- EDIT DOCTOR --------
        [HttpGet]
        public IActionResult EditDoctor(int id)
        {
            var doctor = dc.Doctors.Find(id);

            if (doctor == null)
            {
                return NotFound();
            }
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditDoctor(Doctor doctor)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(doctor.UserPassword))
                {

                    doctor.Password = HashPassword(doctor.UserPassword);
                }

                dc.Doctors.Update(doctor);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        ViewData["Message"] = "Doctor Details Updated successfully";

                    }
                    else
                    {
                        ViewData["v"] = "Doctor Details Edited successfully";

                    }

                }


                catch (Exception ex)
                {

                    ViewData["Message"] = $"An error occurred while updating the doctor: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Model state is invalid.";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ViewData["Message"] += $" {error.ErrorMessage}";
                }
            }
            return View(doctor);
        }

        // ------- DELETE DOCTOR --------
        [HttpGet]
        public IActionResult DeleteDoctor(int id)
        {
            var doctor = dc.Doctors.Find(id);
            if (doctor == null)
            {
                return NotFound();
            }
            return View(doctor);
        }

        [HttpPost, ActionName("DeleteDoctor")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteDoctorConfirmed(int id)
        {
            var doctor = dc.Doctors.Find(id);
            if (doctor != null)
            {
                dc.Doctors.Remove(doctor);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {

                        ViewData["Message"] = "Doctor Details Deleted successfully";
                      
                    }
                    else
                    {
                        ViewData["Message"] = "Failed to delete doctor.";
                    }
                }
                catch (Exception ex)
                {
                   
                    ViewData["Message"] = $"An error occurred while deleting the doctor: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Doctor not found.";
            }
            return View(doctor);
        }

        public IActionResult Doctor()
        {
            return View();
        }



        [HttpGet]
        public IActionResult PharmaRegister()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateDNTCaptcha(ErrorMessage = "Please Enter Valid Captcha",
            CaptchaGeneratorLanguage = Language.English,
            CaptchaGeneratorDisplayMode = DisplayMode.ShowDigits)]
        public IActionResult PharmaRegister(Pharmacist p)
        {
            if (ModelState.IsValid)
            {
                if (!_validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.ShowDigits))
                {
                    this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
                }
                p.Password = HashPassword(p.UserPassword);

                dc.Pharmacists.Add(p);
                int i = dc.SaveChanges();

                if (i > 0)
                {
                    ViewData["v"] = "Pharmacist Registered successfully";
                }
                else
                {
                    ViewData["v"] = "Error occurred while creating pharmacist";
                }
            }
            else
            {
                ViewData["v"] = "Validation failed";
            }
            return View(p);
        }
        // ------- PHARMACIST REGISTRATION LIST --------


        public IActionResult PharmacistRegistration()
        {

            var pharmacists = dc.Pharmacists.ToList();
            return View(pharmacists);
        }

        // ------- EDIT PHARMACIST --------
        [HttpGet]
        public IActionResult EditPharmacist(int id)
        {
            var pharmacist = dc.Pharmacists.Find(id);
            if (pharmacist == null)
            {
                return NotFound();
            }
            return View(pharmacist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPharmacist(Pharmacist pharmacist)
        {
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(pharmacist.UserPassword))
                {
                    pharmacist.Password = HashPassword(pharmacist.UserPassword);
                }

                dc.Pharmacists.Update(pharmacist);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        ViewData["Message"] = "Pharmacist Details Updated successfully";
                    }
                    else
                    {
                        ViewData["Message"] = "Failed to update pharmacist.";
                    }
                }
                catch (Exception ex)
                {
                    ViewData["Message"] = $"An error occurred while updating the pharmacist: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Model state is invalid.";
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    ViewData["Message"] += $" {error.ErrorMessage}";
                }
            }
            return View(pharmacist);
        }

        // ------- DELETE PHARMACIST --------
        [HttpGet]
        public IActionResult DeletePharmacist(int id)
        {
            var pharmacist = dc.Pharmacists.Find(id);
            if (pharmacist == null)
            {
                return NotFound();
            }
            return View(pharmacist);
        }

        [HttpPost, ActionName("DeletePharmacist")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePharmacistConfirmed(int id)
        {
            var pharmacist = dc.Pharmacists.Find(id);
            if (pharmacist != null)
            {
                dc.Pharmacists.Remove(pharmacist);
                try
                {
                    int result = dc.SaveChanges();
                    if (result > 0)
                    {
                        ViewData["Message"] = "Pharmacist details deleted successfully.";
                      
                    }
                    else
                    {
                        ViewData["Message"] = "Failed to delete pharmacist.";
                    }
                }
                catch (Exception ex)
                {
                   
                    ViewData["Message"] = $"An error occurred while deleting the pharmacist: {ex.Message}";
                }
            }
            else
            {
                ViewData["Message"] = "Pharmacist not found.";
            }
            return View(pharmacist);
        }

        public IActionResult Pharmacist()
        {
            return View();
        }

        [HttpGet]
        public IActionResult FrontOfficeRegister()
        {
            return View();
        }

        [HttpPost]

        public IActionResult FrontOfficeRegister(FrontOffice p)
        {
            if (ModelState.IsValid)
            {

                p.Password = HashPassword(p.UserPassword);

                dc.FrontOffices.Add(p);
                int i = dc.SaveChanges();

                if (i > 0)
                {
                    ViewData["v"] = "FrontOfficeMember created successfully";
                }
                else
                {
                    ViewData["v"] = "Error occurred while creating FrontOfficeMember ";
                }
            }
            else
            {
                ViewData["v"] = "Validation failed";
            }
            return View(p);

        }


        public IActionResult FrontOffice()
        {

        return View(); 
        }
    }
}
