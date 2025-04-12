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

        [HttpPost]
        public async Task<IActionResult> CreateAdminRequest(AdminRequestFormViewModel model)
        {
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

            var userId = User.GetId();
            var user = _databaseContext.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

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
                RequestDate = DateTime.UtcNow,
                Status = AdminRequestStatus.Pending
            };

            // Add the admin request to the database
            _databaseContext.AdminRequests.Add(adminRequest);
            await _databaseContext.SaveChangesAsync();

            // Redirect to a success page or where you need
            return RedirectToAction("Index", "Home"); // Or another page after successful request
        }
    }
}
