using Microsoft.AspNetCore.Mvc;

namespace CerVer.API.Controllers
{
    public class CertificatesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
