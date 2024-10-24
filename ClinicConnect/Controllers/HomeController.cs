using ClinicConnect.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Security.Cryptography;
using Serilog;
using ClinicConnect.ViewModels;
using Microsoft.EntityFrameworkCore;


namespace ClinicConnect.Controllers
{
    public class HomeController : Controller
    {



public IActionResult index()
        {
            return View();
        }

    }
}




