using ClinicConnect.Models;
using DNTCaptcha.Core.Providers;
using DNTCaptcha.Core;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Security.Cryptography;
namespace ClinicConnect.Controllers
{
    public class DoctorController : Controller
    {
        ClinicConnectContext dc=new ClinicConnectContext();
        public IActionResult HomeDoctor()
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

        public IActionResult Login()
        {
            return View();
        }


        [HttpPost]     
        public IActionResult Login(Doctor d)
        {
          

            var hashedInputPassword = HashPassword(d.UserPassword);

            var res = (from t in dc.Doctors
                       where t.DoctorEmail == d.DoctorEmail && t.Password.SequenceEqual(hashedInputPassword)
                       select t).FirstOrDefault();

            if (res != null)
            {
              

                HttpContext.Session.SetInt32("DoctorId", res.DoctorId);

                return RedirectToAction("GetApprovedAppointments");
            }
            else
            {
                ViewData["v"] = "Invalid username or password";
            }

            return View();
        }




        public IActionResult GetApprovedAppointments()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
         

            var approvedAppointments = dc.Appointments
                .Where(a => a.AppointmentStatus == true && a.DoctorId == doctorId) 
                .Include(a => a.Patient) 
                .ToList();

            return View(approvedAppointments);
        }



        public IActionResult ViewMessages()
        {
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            var messages = dc.Messages
                .Include(m => m.Patient) 
                .Where(m => m.DoctorId == doctorId) 
                .Select(m => new
                {
                    MessageID = m.MessageId,
                    PatientID = m.PatientId,
                    PatientName = m.Patient.PatientName,
                    PatientsMessage = m.PatientMessage,
                    DoctorsMessage = m.DoctorMessage, 
                    SentDate = m.SentDate
                })
                .ToList();

            return View(messages);
        }




        [HttpGet]
        public IActionResult RespondToMessage(int id)
        {
            var message = dc.Messages
                .Include(m => m.Patient) 
                .Where(m => m.MessageId == id)
                .Select(m => new
                {
                    MessageID = m.MessageId,
                    PatientID = m.PatientId,
                    PatientName = m.Patient.PatientName,
                    PatientsMessage = m.PatientMessage,
                    DoctorsMessage = m.DoctorMessage, 
                    SentDate = m.SentDate
                })
                .FirstOrDefault();

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }



        [HttpPost]
        public IActionResult RespondToMessage(int id, string response)
        {
            var message = dc.Messages.Find(id);
            if (message == null)
            {
                return NotFound();
            }

        
            message.DoctorMessage = response;
            dc.SaveChanges();

            return RedirectToAction(nameof(ViewMessages));
        }

  
        
        [HttpGet]
        public IActionResult SearchMedicine()
        {
            var medicines = dc.Medicines
                .Select(m => new
                {
                    m.MedName,
                    m.MedAvailability
                })
                .ToList();

            return View(medicines);
        }

        [HttpPost]
        public IActionResult SearchMedicine(string medName)
        {
            var medicines = dc.Medicines
                .Where(m => m.MedName.Contains(medName))
                .Select(m => new
                {
                    m.MedName,
                    m.MedAvailability
                })
                .ToList();

            return View(medicines);
        }

    }
}
