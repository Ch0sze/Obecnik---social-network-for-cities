using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    [Route("post")]
    public class CreatePostController : Controller
    {
        [HttpGet("create")]
        public IActionResult Create()
        {
            return PartialView("_CreatePost");  // Returns the modal
        }
    }
}