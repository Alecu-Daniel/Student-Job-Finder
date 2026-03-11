using Microsoft.AspNetCore.Mvc;

namespace Student_Job_Finder.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index(string viewType = "student")
        {
            ViewBag.ActiveView = viewType;
            return View();
        }
    }
}
