using Microsoft.AspNetCore.Mvc;

namespace OOOControlSystem.Controllers
{
    public class DefectsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
