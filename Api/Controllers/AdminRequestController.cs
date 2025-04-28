using Microsoft.AspNetCore.Mvc;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Application.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Api.Extensions;

namespace Application.Api.Controllers
{
    [Route("adminrequest")]
    public class AdminRequestController : Controller
    {
        private readonly DatabaseContext _databaseContext;

        public AdminRequestController(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        // New version of the method without authorization
        [HttpGet("requestadminrights")]
        public IActionResult RequestAdminRightsNoAuth()
        {
            // Fetch communities from the database
            var communities = _databaseContext.Communities.ToList();

            // Fetch admin
            var user = _databaseContext.Users.FirstOrDefault(u => u.Email == "obecnika@gmail.com");// Assuming ID 1 corresponds to the first created user

            // Create the view model and populate the Communities list and User with ID 1
            var model = new AdminRequestFormViewModel
            {
                Communities = communities, // Add communities to the view model
                User = user // Add the user with ID 1 to the view model
            };

            return View("~/Views/Account/RequestAdminRights.cshtml", model);
        }

        // The existing POST method to create admin requests
        [HttpPost]
        public async Task<IActionResult> CreateAdminRequest(AdminRequestFormViewModel model)
        {
            Console.WriteLine("CreateAdminRequest POST method is called");
            // Server-side validation
            if (string.IsNullOrWhiteSpace(model.OfficialEmail))
            {
                ModelState.AddModelError("OfficialEmail", "Je potřeba zadat oficiální email");
            }
            else if (model.OfficialEmail.Length > 100)
            {
                ModelState.AddModelError("OfficialEmail", "Oficiální email může mít maximálně 100 znaků");
            }

            if (model.Notes?.Length > 500)
            {
                ModelState.AddModelError("Notes", "Poznámky mohou mít maximálně 500 znaků");
            }

            if (!ModelState.IsValid)
            {
                model.Communities = _databaseContext.Communities.ToList();
                // Return to the form with errors
                return View("~/Views/Account/RequestAdminRights.cshtml", model);
            }

            var user = _databaseContext.Users.FirstOrDefault(u => u.Email == "obecnika@gmail.com");
            if (user == null)
            {
                // If no user found, redirect to the login page or handle accordingly
                return RedirectToAction("Register", "Account");
            }

            // Use the CommunityId passed from the form
            var communityId = model.CommunityId;

            // Ensure the community exists in the database
            var community = _databaseContext.Communities.FirstOrDefault(c => c.Id == communityId);
            if (community == null)
            {
                model.Communities = _databaseContext.Communities.ToList();
                ModelState.AddModelError("CommunityId", "Komunita nebyla nalezena");
                return View("~/Views/Account/RequestAdminRights.cshtml", model);
            }

            // Create the admin request object
            var adminRequest = new AdminRequestDo
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CommunityId = communityId,
                OfficialEmail = model.OfficialEmail,
                Notes = model.Notes,
                Population = model.Population,
                RequestDate = DateTime.UtcNow,
                Status = AdminRequestStatus.Pending
            };

            // Add the admin request to the database
            _databaseContext.AdminRequests.Add(adminRequest);
            await _databaseContext.SaveChangesAsync();

            // Redirect to a success page or where you need
            var successModel = new AdminRequestSuccessViewModel
            {
                OfficialEmail = model.OfficialEmail,
                CommunityName = community.Name // Assuming "Name" is the name property of your Community
            };
            
            return View("Views/Home/AdminRequestSuccess.cshtml", successModel);
        }
    }
}
