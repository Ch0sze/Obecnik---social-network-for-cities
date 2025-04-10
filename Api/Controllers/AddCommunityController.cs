using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers
{
    public class AddCommunitiesController : Controller
    {
        // This action method will handle the form submission
        [HttpPost]
        public IActionResult Join(string communityCode)
        {
            // Example logic: If you want to check if the community code is valid
            if (string.IsNullOrEmpty(communityCode))
            {
                // Optionally return an error message or redirect
                ModelState.AddModelError("CommunityCode", "Community code is required.");
                return View();  // You might want to render the view again with an error message
            }

            // Your logic for handling the community join goes here
            // For example, check if the community exists in the database

            // After successfully joining the community, redirect or render a new view
            return RedirectToAction("Index", "Home"); // Or whatever action you want after success
        }
    }
}