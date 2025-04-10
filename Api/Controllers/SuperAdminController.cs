using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Application.Api.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace Application.Api.Controllers
{
    [Route("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class SuperAdminController(DatabaseContext databaseContext) : Controller
    {


        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = User.GetId();
            var currentUser = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (currentUser == null)
            {
                return RedirectToAction("Error", "Home");
            }

            var requests = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .Where(r => r.Status == AdminRequestStatus.Pending)
                .ToListAsync();
            
            var id = User.GetId();
            var user = databaseContext.Users.FirstOrDefault(user => user.Id == id);
            
            var community = databaseContext.UserCommunities
                .Include(uc => uc.Community)
                .FirstOrDefault(uc => user != null && uc.UserId == user.Id)?.Community;

            if (community != null)
            {
                var model = new CombinedViewModel
                {
                
                    AccountViewModel = new AccountViewModel
                    {
                        Email = currentUser.Email ?? string.Empty,
                        Name = $"{currentUser.Firstname} {currentUser.LastName}",
                        Hometown = $"{currentUser.Residence}, {currentUser.PostalCode}"
                    },
                    AdminRequestViewModel = requests.Select(r => new AdminRequestFormViewModel
                    {
                        Id = r.Id,
                        OfficialEmail = r.OfficialEmail,
                        RequestDate = r.RequestDate,
                        Community = r.Community,
                        User = r.User,
                        Notes = r.Notes
                    }).ToList(),
                
                    HomeViewModel = new HomeViewModel
                    {
                        CommunityId = community.Id,
                        CommunityName = community.Name,
                        Posts = []
                    }
                };


                return View("Index", model);
            }

            return View("Index");
        }


        [HttpGet("requests")]
        public async Task<IActionResult> ViewSuperAdminRequests()
        {
            var requests = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .Where(r => r.Status == AdminRequestStatus.Pending)
                .ToListAsync();

            var model = new CombinedViewModel
            {
                AdminRequestViewModel = requests.Select(r => new AdminRequestFormViewModel
                {
                    Id = r.Id,
                    OfficialEmail = r.OfficialEmail,
                    RequestDate = r.RequestDate,
                    Community = r.Community,
                    User = r.User,
                    Notes = r.Notes
                }).ToList()
            };

            return View("Index", model);
        }

        [HttpPost("accept/{id}")]
        public async Task<IActionResult> AcceptSuperAdminRequest(Guid id)
        {
            var request = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return RedirectToAction("Index");
            }

            // Accept the request and update the user role
            request.User.Role = "Admin";
            request.Status = AdminRequestStatus.Approved;

            databaseContext.AdminRequests.Update(request);
            await databaseContext.SaveChangesAsync();

            // After accepting the request, fetch the updated requests and user data
            var updatedRequests = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .Where(r => r.Status == AdminRequestStatus.Pending)
                .ToListAsync();

            // Get updated user data and their community
            var updatedUser = await databaseContext.Users
                .Include(u => u.UserCommunities)
                .ThenInclude(uc => uc.Community)
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            // Get the community the user belongs to
            var community = updatedUser?.UserCommunities
                .Select(uc => uc.Community)
                .FirstOrDefault();

            var updatedModel = new CombinedViewModel
            {
                AccountViewModel = new AccountViewModel
                {
                    Email = request.User.Email ?? string.Empty,
                    Name = $"{request.User.Firstname} {request.User.LastName}",
                    Hometown = $"{request.User.Residence}, {request.User.PostalCode}"
                },
                AdminRequestViewModel = updatedRequests.Select(r => new AdminRequestFormViewModel
                {
                    Id = r.Id,
                    OfficialEmail = r.OfficialEmail,
                    RequestDate = r.RequestDate,
                    Community = r.Community,
                    User = r.User,
                    Notes = r.Notes
                }).ToList(),

                // Update HomeViewModel with community data
                HomeViewModel = new HomeViewModel
                {
                    CommunityId = community?.Id ?? Guid.Empty,
                    CommunityName = community?.Name ?? "Neznámá obec",
                    Posts = []
                }
            };

            return View("Index", updatedModel);
        }


        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectSuperAdminRequest(Guid id)
        {
            var request = await databaseContext.AdminRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return RedirectToAction("Index");
            }

            databaseContext.AdminRequests.Remove(request);
            await databaseContext.SaveChangesAsync();

            // Fetch the latest requests and community data after rejection
            var updatedRequests = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .Where(r => r.Status == AdminRequestStatus.Pending)
                .ToListAsync();

            // Fetch updated user data and their community
            var user = await databaseContext.Users
                .Include(u => u.UserCommunities)
                .ThenInclude(uc => uc.Community)
                .FirstOrDefaultAsync(u => u.Id == User.GetId());

            var community = user?.UserCommunities
                .Select(uc => uc.Community)
                .FirstOrDefault();

            var updatedModel = new CombinedViewModel
            {
                AdminRequestViewModel = updatedRequests.Select(r => new AdminRequestFormViewModel
                {
                    Id = r.Id,
                    OfficialEmail = r.OfficialEmail,
                    RequestDate = r.RequestDate,
                    Community = r.Community,
                    User = r.User,
                    Notes = r.Notes
                }).ToList(),

                // Update HomeViewModel with community data
                HomeViewModel = new HomeViewModel
                {
                    CommunityId = community?.Id ?? Guid.Empty,
                    CommunityName = community?.Name ?? "Neznámá obec",
                    Posts = []
                }
            };

            return View("Index", updatedModel);
        }


    }
}
