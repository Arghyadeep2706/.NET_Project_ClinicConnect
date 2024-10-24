using ClinicConnect.Models;
using DNTCaptcha.Core;
using DNTCaptcha.Core.Providers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace ClinicConnect.Controllers
{

    public class FrontOfficeController : Controller
    {
      
        ClinicConnectContext dc = new ClinicConnectContext();
        public readonly IDNTCaptchaValidatorService _validatorService;

        public FrontOfficeController(IDNTCaptchaValidatorService validatorService)
        {
            _validatorService = validatorService;
        }

        public IActionResult HomeFrontOffice()
        {
            return View();
        }
       
        public IActionResult ViewAllAppointments()
        {
            var res = from t in dc.Appointments
                    
                      select t;

            return View(res);
        }

        [HttpGet]
        public IActionResult ApproveAppoint()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ApproveAppoint(int id)
        {
            var appointment1 = dc.Appointments.Find(id);
            if (appointment1 == null)
            {
                return NotFound();
            }
            appointment1.AppointmentStatus = true;
            dc.SaveChangesAsync();
            return RedirectToAction("HomeFrontOffice");
        }

        public IActionResult Availability()
        {
            var res = dc.Doctors.Include(d => d.DoctorAvailabilities).ToList(); 
            return View(res); 
        }

        public IActionResult CancelAppointment()
        {
            var futureAppointments = dc.Appointments
                .Include(a => a.Patient) 
                .Where(a => a.AppointmentDate > DateTime.Now && !a.Messages.Any()) 
                .ToList();

            return View(futureAppointments);
        }

        [HttpPost]
        public IActionResult CancelAppointment(int id)
        {
            var appointment = dc.Appointments.Find(id);
            if (appointment == null)
            {
                return NotFound();
            }

            dc.Appointments.Remove(appointment);
            dc.SaveChanges();
            TempData["Message"] = "The appointment has been canceled.";

            return RedirectToAction("CancelAppointment"); 
        }

        public IActionResult Book()
        {
            ViewBag.Patients = dc.Patients.ToList();
            ViewBag.Doctors = dc.Doctors.ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Book(int patientId, int doctorId, DateTime appointmentDate, string reason)
        {
            var existingAppointment = dc.Appointments
                .FirstOrDefault(a => a.PatientId == patientId);

            if (existingAppointment == null)
            {
                var newAppointment = new Appointment
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    AppointmentDate = appointmentDate,
                    AppointmentStatus = true,
                    Reason = reason
                };

                dc.Add(newAppointment);
                dc.SaveChangesAsync();
                TempData["Message"] = "Appointment booked successfully!";
            }
            else
            {
                TempData["Message"] = "This patient's appointment is already in progress.";
            }

            return RedirectToAction(nameof(ViewAllAppointments));
        }
        public IActionResult Show()
        {
            var medicines = dc.Medicines.ToList();
            return View(medicines);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var appoint = dc.Appointments.Find(id);
            if (appoint == null)
            {
                return NotFound();
            }
            return View();
        }

        [HttpPost]
        public IActionResult Edit(Appointment a)
        {
            if (ModelState.IsValid)
            {
                var existingMedicine = dc.Appointments.Find(a.AppointmentId);
                if (existingMedicine != null)
                {
                    existingMedicine.PatientId = a.PatientId;
                    existingMedicine.AppointmentId = a.AppointmentId;
                    existingMedicine.AppointmentDate = a.AppointmentDate;

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
            return View(a);

        }

        [HttpGet]
        public IActionResult SearchAppoint()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SearchAppoint(int appointmentId)
        {
            var today = DateTime.Now;
            var appointment = await dc.Appointments
                .Where(a => a.AppointmentId == appointmentId && a.AppointmentDate > today)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                ViewBag.Message = "No future appointments found.";
                return View(); 
            }
            return RedirectToAction(nameof(EditAppoint), new { id = appointment.AppointmentId });
        }

        public async Task<IActionResult> EditAppoint(int id)
        {
            var appointment = await dc.Appointments
                .Include(a => a.Doctor) 
                .FirstOrDefaultAsync(a => a.AppointmentId == id);

            if (appointment == null)
            {
                return NotFound();
            }
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppoint(int id, DateTime newAppointmentDate)
        {
            var appointment = await dc.Appointments.FindAsync(id);
            if (appointment == null)
            {
                return NotFound();
            }

            if (newAppointmentDate <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "You cannot reschedule to a past date.";
                return View(appointment);
            }

            appointment.AppointmentDate = newAppointmentDate;

            try
            {
                await dc.SaveChangesAsync();
                TempData["SuccessMessage"] = "Appointment date updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.AppointmentId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(SearchAppoint));
        }
        private bool AppointmentExists(int id)
        {
            return dc.Appointments.Any(e => e.AppointmentId == id);
        }
        public static byte[] HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateDNTCaptcha(ErrorMessage = "Please Enter Valid Captcha",
    CaptchaGeneratorLanguage = Language.English,
    CaptchaGeneratorDisplayMode = DisplayMode.ShowDigits)]
        public IActionResult Login(FrontOffice p)
        {
            if (!_validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.ShowDigits))
            {
                this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
            }
            var hashedInputPassword = HashPassword(p.UserPassword);

            var res = (from t in dc.FrontOffices
                       where t.FrontOfficeEmail == p.FrontOfficeEmail && t.Password.SequenceEqual(hashedInputPassword)
                       select t).FirstOrDefault();

            if (res != null)
            {
                var userClaims = new List<Claim>
{
    new Claim(ClaimTypes.Email, res.FrontOfficeEmail),
    new Claim(ClaimTypes.Name, res.FrontOfficeName),
     new Claim("FrontOfficeMemberCode", res.FrontOfficeMemberCode.ToString())
};
                var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                HttpContext.Session.SetInt32("FrontOfficeMemberCode", res.FrontOfficeMemberCode);
                return RedirectToAction("ViewAllAppointments");
            }
            else
            {
                ViewData["v"] = "Invalid username or password";
            }

            return View();
        }

        public IActionResult Logout()
        {
            var login = HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult EditProfile()
        {
            String email = HttpContext.Session.GetString("uid");
            var userId = (from t in dc.FrontOffices
                          where t.FrontOfficeEmail == email
                          select t.FrontOfficeMemberCode).FirstOrDefault();
            var user = dc.FrontOffices.FirstOrDefault(u => u.FrontOfficeMemberCode == userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public IActionResult EditProfile(FrontOffice user)
        {
            try
            {
                String email = HttpContext.Session.GetString("uid");
                var userId = (from t in dc.FrontOffices
                              where t.FrontOfficeEmail == email
                              select t.FrontOfficeMemberCode).FirstOrDefault();
                var existingUser = dc.FrontOffices.FirstOrDefault(u => u.FrontOfficeMemberCode == userId);
                if (existingUser == null)
                {
                    return NotFound();
                }
                existingUser.FrontOfficeName = user.FrontOfficeName;
                existingUser.FrontOfficeEmail = user.FrontOfficeEmail;
                existingUser.FrontOfficePhoneNo = user.FrontOfficePhoneNo;
                existingUser.FrontOfficeAddress = user.FrontOfficeAddress;

                if (user.Password != null && user.Password.Length > 0)
                {
                    existingUser.Password = user.Password;
                }
                dc.FrontOffices.Update(existingUser);
                dc.SaveChanges();
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ModelState.AddModelError("", "An error occurred while updating the profile.");
            }
            return View(user);
        }
    }
}



    

