using Microsoft.AspNetCore.Mvc;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Application.Api.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Api.Extensions;
using Microsoft.EntityFrameworkCore;

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

        // POST method to handle form submission
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
                // Re-fetch the communities list when there are validation errors
                var communities = await _databaseContext.Communities.ToListAsync();
                model.Communities = communities; // Pass the communities back to the view
                return View(model); // Return the view with error
            }

            var userId = User.GetId();
            var user = await _databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Use the CommunityId passed from the form
            var communityId = model.CommunityId;

            // Ensure the community exists in the database
            var community = await _databaseContext.Communities
                .FirstOrDefaultAsync(c => c.Id == communityId);
            if (community == null)
            {
                ModelState.AddModelError("CommunityId", "Komunita nebyla nalezena");
                var communities = await _databaseContext.Communities.ToListAsync();
                model.Communities = communities;
                return View(model); // Return to form with error
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

        public IActionResult Account()
        {
            var model = new AccountViewModel();
            return View("Account", model);
        }
    }
}
