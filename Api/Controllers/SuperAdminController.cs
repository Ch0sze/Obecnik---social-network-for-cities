using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Application.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
            // Need to add EmailSender for sending info about creating account (if email doesnt exit => link to change password)
            // If it does exist give account UnpaidAdmin role => Send email about this info
            // After payment change UnpaidAdmin to Admin


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

            var existingUser = await databaseContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.OfficialEmail);

            if (existingUser == null)
            {
                // Generate random password
                var randomPassword = Guid.NewGuid().ToString("N")[..12];

                // Hash and salt
                using var hmac = new HMACSHA512();
                var passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(randomPassword));
                var passwordSalt = hmac.Key;

                var newUser = new UserDo
                {
                    Id = Guid.NewGuid(),
                    Email = request.OfficialEmail,
                    Firstname = "Nový",
                    LastName = "Admin",
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    Role = "UnpaidAdmin",
                    Residence = request.Community.Name,
                    PostalCode = request.Community.PostalCode,
                };

                await databaseContext.Users.AddAsync(newUser);

                // Assign user to community
                var userCommunity = new UserCommunityDo
                {
                    UserId = newUser.Id,
                    CommunityId = request.CommunityId,
                    User = newUser,
                    Community = request.Community
                };

                await databaseContext.UserCommunities.AddAsync(userCommunity);

                // (Optional) Send email with login instructions or password reset
            }
            else
            {
                // Update role if user already exists
                existingUser.Role = "UnpaidAdmin";

                var alreadyAssigned = await databaseContext.UserCommunities
                    .AnyAsync(uc => uc.UserId == existingUser.Id && uc.CommunityId == request.CommunityId);

                if (!alreadyAssigned)
                {
                    var userCommunity = new UserCommunityDo
                    {
                        UserId = existingUser.Id,
                        CommunityId = request.CommunityId,
                        User = existingUser,
                        Community = request.Community
                    };

                    await databaseContext.UserCommunities.AddAsync(userCommunity);
                }
            }

            // Change the request status to Approved
            request.Status = AdminRequestStatus.Approved;

            // Save changes to the database
            await databaseContext.SaveChangesAsync();

            return RedirectToAction("Index");
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

            var updatedRequests = await databaseContext.AdminRequests
                .Include(r => r.User)
                .Include(r => r.Community)
                .Where(r => r.Status == AdminRequestStatus.Pending)
                .ToListAsync();

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

                HomeViewModel = new HomeViewModel
                {
                    CommunityId = community?.Id ?? Guid.Empty,
                    CommunityName = community?.Name ?? "Neznámá obec",
                    Posts = []
                }
            };

            return View("Index", updatedModel);
        }

        [HttpGet("Users")]
        public async Task<IActionResult> ManageUsers(string searchQuery)
        {
            var currentUserId = User.GetId();
            var currentUser = await databaseContext.Users
                .Include(u => u.UserCommunities)
                .ThenInclude(uc => uc.Community)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if the user is banned
            if (currentUser.Role == "Banned")
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                TempData["ErrorMessage"] = "Váš účet byl zabanován.";

                return RedirectToAction("Login", "Account");
            }


            var usersQuery = databaseContext.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Firstname.Contains(searchQuery) || u.LastName.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery));
            }

            var users = await usersQuery.ToListAsync();

            var model = new CombinedViewModel
            {
                Users = users,
                SearchQuery = searchQuery,
                AccountViewModel = new AccountViewModel
                {
                    Email = currentUser?.Email ?? string.Empty,
                    Name = $"{currentUser?.Firstname} {currentUser?.LastName}",
                    Hometown = $"{currentUser?.Residence}, {currentUser?.PostalCode}",
                    Role = currentUser?.Role
                },
                HomeViewModel = new HomeViewModel
                {
                    CommunityId = currentUser?.UserCommunities?.FirstOrDefault()?.Community.Id ?? Guid.Empty,
                    CommunityName = currentUser?.UserCommunities?.FirstOrDefault()?.Community.Name ?? "Unknown",
                    Posts = []
                }
            };

            return View("ManageUsers", model);
        }

        [HttpGet("Admins")]
        public async Task<IActionResult> ManageAdmins(string searchQuery)
        {
            var currentUserId = User.GetId();
            var currentUser = await databaseContext.Users
                .Include(u => u.UserCommunities)
                .ThenInclude(uc => uc.Community)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (currentUser.Role == "Banned")
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["ErrorMessage"] = "Váš účet byl zabanován.";
                return RedirectToAction("Login", "Account");
            }

            var usersQuery = databaseContext.Users
                .Include(u => u.UserCommunities)
                .ThenInclude(uc => uc.Community)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Firstname.Contains(searchQuery) ||
                    u.LastName.Contains(searchQuery) ||
                    u.Email.Contains(searchQuery));
            }

            var users = await usersQuery.ToListAsync();

            var model = new CombinedViewModel
            {
                Users = users, // Include all, filter in view
                SearchQuery = searchQuery,
                AccountViewModel = new AccountViewModel
                {
                    Email = currentUser.Email,
                    Name = $"{currentUser.Firstname} {currentUser.LastName}",
                    Hometown = $"{currentUser.Residence}, {currentUser.PostalCode}",
                    Role = currentUser.Role
                },
                HomeViewModel = new HomeViewModel
                {
                    CommunityId = currentUser.UserCommunities.FirstOrDefault()?.Community?.Id ?? Guid.Empty,
                    CommunityName = currentUser.UserCommunities.FirstOrDefault()?.Community?.Name ?? "Unknown",
                    Posts = []
                }
            };

            return View("ManageAdmins", model);
        }


        [HttpPost("ban/{id}")]
        public async Task<IActionResult> BanUser(Guid id)
        {
            var user = await databaseContext.Users.FindAsync(id);

            if (user == null || user.Role == "SuperAdmin")
                return RedirectToAction("ManageUsers");

            user.Role = "Banned";
            await databaseContext.SaveChangesAsync();

            return RedirectToAction("ManageUsers");
        }

        [HttpPost("unban/{id}")]
        public async Task<IActionResult> UnbanUser(Guid id)
        {
            var user = await databaseContext.Users.FindAsync(id);

            if (user == null)
                return RedirectToAction("ManageUsers");

            user.Role = "User";
            await databaseContext.SaveChangesAsync();

            return RedirectToAction("ManageUsers");
        }
    }
}