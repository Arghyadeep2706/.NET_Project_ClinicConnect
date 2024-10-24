using ClinicConnect.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Channels;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using DNTCaptcha.Core;
using DNTCaptcha.Core.Providers;
using Microsoft.AspNetCore.Authorization;

namespace ClinicConnect.Controllers
{
 
    public class PatientController : Controller
    {
        ClinicConnectContext dc= new ClinicConnectContext();


        public readonly IDNTCaptchaValidatorService _validatorService;

        public PatientController(IDNTCaptchaValidatorService validatorService)
        {
           
            _validatorService = validatorService;
           
        }


       
     
        public IActionResult HomePatient()
        {         
            return View();
        }




        public static byte[] HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }



        [HttpGet]
        public IActionResult RegisterPatient()
        {
            
            return View();
        }

    
        public IActionResult RegisterPatient(Patient patient)
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



        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        
        [ValidateDNTCaptcha(ErrorMessage = "Please Enter Valid Captcha",
    CaptchaGeneratorLanguage = Language.English,
    CaptchaGeneratorDisplayMode = DisplayMode.ShowDigits)]
        public IActionResult Login(Patient p)
        {
            if (!_validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.ShowDigits))
            {
                this.ModelState.AddModelError(DNTCaptchaTagHelper.CaptchaInputName, "Please Enter Valid Captcha.");
            }

         
            var hashedInputPassword = HashPassword(p.UserPassword);

            var res = (from t in dc.Patients
                       where t.PatientEmail == p.PatientEmail && t.Password.SequenceEqual(hashedInputPassword)
                       select t).FirstOrDefault();

            if (res != null)
            {
                var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, res.PatientEmail),
            new Claim(ClaimTypes.Name, res.PatientName),
            new Claim("PatientId", res.PatientId.ToString()) // Store Patient ID in claims
        };

                var identity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                HttpContext.Session.SetInt32("PatientId", res.PatientId);

                return RedirectToAction("HomePatient");
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


        public IActionResult ViewDoctorVisitation()
        {
            var doctors = dc.Doctors
                            .Include(d => d.DoctorAvailabilities) 
                            .ToList();

            return View(doctors);
        }


        [Authorize]
        public IActionResult BookAppointment()
        {
           
            ViewBag.Doctors = dc.Doctors.ToList(); 
            return View();
        }

       
        [HttpPost]
        public IActionResult BookAppointment(Appointment appointment)
        {
           
            var patientId = HttpContext.Session.GetInt32("PatientId");

           
            if (ModelState.IsValid)
            {
             
                if (appointment.AppointmentDate > DateTime.Now)
                {
                    appointment.PatientId = patientId.Value; 
                    appointment.AppointmentStatus = false; 

                    dc.Appointments.Add(appointment);
                    dc.SaveChanges();

                    TempData["SuccessMessage"] = "Your appointment has been booked successfully!";

                  
                    ViewBag.Doctors = dc.Doctors.ToList();
                    return View(); 
                }
                else
                {
                    ModelState.AddModelError("", "You cannot book an appointment for a date in the past.");
                }
            }

            
            ViewBag.Doctors = dc.Doctors.ToList();
            return View(appointment);
        }




        [Authorize]
        public IActionResult RescheduleAppointment() 
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
           
            var futureAppointments = dc.Appointments
                .Include(a => a.Doctor) 
                .Where(a => a.PatientId == patientId && a.AppointmentDate > DateTime.Now)
                .ToList();

            return View(futureAppointments); 
        }

       
        [HttpPost]
        public IActionResult RescheduleAppointment(int appointmentId, DateTime newAppointmentDate, string newReason)
        {
            var appointment = dc.Appointments.Find(appointmentId);
            if (appointment != null)
            {
                if (newAppointmentDate > DateTime.Now)
                {
                    appointment.AppointmentDate = newAppointmentDate;
                    appointment.Reason = newReason; 
                    dc.SaveChanges();
                    TempData["SuccessMessage"] = "Appointment rescheduled successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "You cannot reschedule to a past date.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Appointment not found.";
            }

          
            return RedirectToAction("RescheduleAppointment", new { patientId = appointment?.PatientId });
        }





        [Authorize]
        public IActionResult CancelAppointment() 
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
         
            var futureAppointments = dc.Appointments
                .Include(a => a.Doctor) 
                .Where(a => a.PatientId == patientId
                            && a.AppointmentDate > DateTime.Now
                            && !dc.Messages.Any(m => m.PatientId == patientId && m.DoctorId == a.DoctorId)) 
                .ToList();

            return View(futureAppointments); 
        }


        [HttpPost]
        public IActionResult SubmitCancelAppointment(int appointmentId) 
        {
            var appointment = dc.Appointments.Find(appointmentId);
            if (appointment != null)
            {
              
                dc.Appointments.Remove(appointment);
                dc.SaveChanges();
                TempData["SuccessMessage"] = "Appointment canceled successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Appointment not found.";
            }

          
            return RedirectToAction("CancelAppointment", new { patientId = appointment?.PatientId });
        }









        [Authorize]
        public IActionResult SendMessageToDoctor()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            var recentAppointments = dc.Appointments
                .Where(a => a.PatientId == patientId && a.AppointmentDate >= DateTime.Now.AddDays(-7))
                .ToList();

            var doctorIds = recentAppointments.Select(a => a.DoctorId).Distinct().ToList();
            ViewBag.Doctors = dc.Doctors.Where(d => doctorIds.Contains(d.DoctorId)).ToList();

            return View(new Message());
        }

       
        [Authorize]
        [HttpGet]
        public IActionResult RespondToMessage(int id)
        {
            var message = dc.Messages.Include(m => m.Doctor).FirstOrDefault(m => m.MessageId == id);
            if (message == null)
            {
                return NotFound();
            }

            return View("RespondToMessage", message);
        }



        [HttpPost]
        public IActionResult RespondToMessage(int id, string response)
        {
            var message = dc.Messages.Find(id);
            if (message == null)
            {
                return NotFound();
            }

           
            var messageCount = dc.Messages.Count(msg => msg.PatientId == message.PatientId && msg.DoctorId == message.DoctorId && msg.SentDate >= DateTime.Now.AddDays(-7));

            if (messageCount >= 5)
            {
                TempData["Message"] = "You can only send up to 5 messages within one week for this doctor.";
                return RedirectToAction("RespondToMessage"); 
            }

   
            message.DoctorMessage = response;
            dc.SaveChanges();

            TempData["Message"] = "Your response has been sent to the doctor successfully.";
            return RedirectToAction("RespondToMessage"); 
        }



        [Authorize]
        public IActionResult ViewMessages()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
         
            var recentDoctorIds = dc.Appointments
                .Where(a => a.PatientId == patientId && a.AppointmentDate >= DateTime.Now.AddDays(-7))
                .Select(a => a.DoctorId)
                .Distinct()
                .ToList();

       
            var messages = dc.Messages
                .Where(m => m.PatientId == 2 && recentDoctorIds.Contains(m.DoctorId)) 
                .Include(m => m.Doctor)
                .GroupBy(m => m.DoctorId)
                .Select(g => g.OrderByDescending(m => m.SentDate).FirstOrDefault())
                .ToList() 
                .Select(m => new
                {
                    m.MessageId,
                    m.PatientId,
                    DoctorName = m.Doctor?.DoctorName, 
                    m.PatientMessage,
                    m.DoctorMessage,
                    m.SentDate
                })
                .ToList();

            return View(messages);
        }














        public IActionResult GetAppointmentHistory()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
            
            var appointments = dc.Appointments
                .Where(a => a.PatientId == patientId)
                .Include(a => a.Doctor) 
                .ToList();

            return View(appointments); 
        }




        public IActionResult SearchMed()
        {
            var medicines = dc.Medicines.ToList(); 
            return View(medicines);
        }

        [HttpPost]
        public IActionResult SearchMed(string medName)
        {
            var medicines = dc.Medicines
                .Where(m => m.MedName.Contains(medName))
                .ToList();

            return View(medicines);
        }






        public IActionResult EditProfile()
        {
            var patientId = HttpContext.Session.GetInt32("PatientId");
           
            var patient = dc.Patients.Find(patientId);

            return View(patient); 
        }

        [HttpPost]
        public IActionResult EditProfile(Patient updatedPatient)
        {
            if (ModelState.IsValid)
            {
                
                var existingPatient = dc.Patients.Find(updatedPatient.PatientId);

              
                existingPatient.PatientName = updatedPatient.PatientName;
                existingPatient.PatientAge = updatedPatient.PatientAge;
                existingPatient.PatientGender = updatedPatient.PatientGender;
                existingPatient.Dob = updatedPatient.Dob;
                existingPatient.Password = updatedPatient.Password; 
                existingPatient.PatientEmail = updatedPatient.PatientEmail;
                existingPatient.PatientPhoneNo = updatedPatient.PatientPhoneNo;
                existingPatient.PatientAddress = updatedPatient.PatientAddress;
                existingPatient.SecurityQuestion = updatedPatient.SecurityQuestion;

                
                dc.SaveChanges();

               
                TempData["Message"] = "Your details have been updated successfully.";

                return View(); 
            }

           
            return View(updatedPatient);
        }



    }
}
