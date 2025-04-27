using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Application.Api.Controllers
{
    [AllowAnonymous]
    [Route("landing")]
    public class LandingController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }
    }
}