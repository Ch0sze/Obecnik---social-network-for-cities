using Microsoft.AspNetCore.Mvc;
using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using CoatOfArmsDownloader.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Application.Api.Controllers
{
    public class AddCommunitiesController(DatabaseContext databaseContext) : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Join(JoinCommunityViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Community))
            {
                ModelState.AddModelError("Community", "Musíte vybrat obec ze seznamu.");
                return RedirectToAction("Index", "Home");
            }

            // Safely retrieve the userId from claims
            var userIdClaim = User.FindFirstValue("Id");
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(); // Handle the case where the user is not authenticated
            }

            var userId = Guid.Parse(userIdClaim);
            var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized(); // Handle if user is not found in the database
            }
            
            // Check if user is banned
            if (user?.Role == "Banned")
            {
                // If banned, log them out and redirect to login with banned message
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
            }

            var community = await databaseContext.Communities
                .FirstOrDefaultAsync(c => c.Name == model.Community && c.PostalCode == model.PostalCode);

            if (community == null)
            {
                // Create new community if not found
                community = new CommunityDo
                {
                    Id = Guid.NewGuid(),
                    Name = model.Community,
                    PostalCode = model.PostalCode,
                    Picture = await CoatOfArmsScraper.GetCommunityCoatOfArms(model.Community)
                };

                await databaseContext.Communities.AddAsync(community);
                await databaseContext.SaveChangesAsync();

                // Create the default channel "Obecné"
                var newChannel = new ChannelDo
                {
                    Id = Guid.NewGuid(),
                    Name = "Obecné",
                    CommunityId = community.Id,
                    Community = community
                };
                await databaseContext.Channels.AddAsync(newChannel);
                await databaseContext.SaveChangesAsync();
            }

            var alreadyMember = await databaseContext.UserCommunities
                .AnyAsync(uc => user != null && uc.UserId == user.Id && uc.CommunityId == community.Id);

            if (!alreadyMember)
            {
                // Add user to the community
                if (user != null)
                {
                    var userCommunity = new UserCommunityDo
                    {
                        UserId = user.Id,
                        CommunityId = community.Id,
                        User = user,
                        Community = community
                    };

                    await databaseContext.UserCommunities.AddAsync(userCommunity);
                }

                await databaseContext.SaveChangesAsync();
            }

            TempData["Message"] = "Připojeno ke komunitě!";
            return RedirectToAction("Index", "Home");
        }
    }
}

