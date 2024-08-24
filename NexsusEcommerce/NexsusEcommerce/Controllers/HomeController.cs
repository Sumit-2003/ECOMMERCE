using Microsoft.AspNetCore.Mvc;
using NexsusEcommerce.Models;
using System.Linq;

namespace NexsusEcommerce.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}