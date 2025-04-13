using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;

[Authorize]
[Route("/")]
public class HomeController(DatabaseContext databaseContext) : Controller
{
    [HttpGet]
    public IActionResult Index(Guid? communityId)
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return RedirectToAction("Login", "Account");

        if (user.Role == "Banned")
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
        }

        // fallback to first community if none selected or invalid
        var availableCommunityIds = databaseContext.UserCommunities
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CommunityId)
            .ToList();

        var selectedCommunityId = communityId.HasValue && availableCommunityIds.Contains(communityId.Value)
            ? communityId.Value
            : availableCommunityIds.FirstOrDefault();

        var communityName = databaseContext.Communities
            .Where(c => c.Id == selectedCommunityId)
            .Select(c => c.Name)
            .FirstOrDefault() ?? "No Community";

        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        var isCommunityAdmin = databaseContext.CommunityAdmins
            .Any(ca => ca.UserId == userId && ca.CommunityId == selectedCommunityId);

        var adminRole = user.Role == "UnpaidAdmin";

        var posts = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == channelId)
            .OrderByDescending(post => post.CreatedAt)
            .Take(10)
            .Select(post => new HomeViewModel.Post
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                Photo = post.Photo != null,
                IsAdmin = adminRole && isCommunityAdmin
            })
            .ToList();

        var homeViewModel = new HomeViewModel
        {
            Posts = posts,
            CommunityName = communityName,
            CommunityId = selectedCommunityId
        };

        var accountViewModel = new AccountViewModel
        {
            Email = user?.Email ?? string.Empty,
            Name = $"{user?.Firstname} {user?.LastName}",
            Hometown = $"{user?.Residence}, {user?.PostalCode}"
        };

        var combinedViewModel = new CombinedViewModel
        {
            AccountViewModel = accountViewModel,
            HomeViewModel = homeViewModel
        };

        return View(combinedViewModel);
    }

    
    [HttpGet("load-posts")]
    public IActionResult LoadPosts(int pageNumber, int pageSize, Guid? communityId)
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        // Verify selected community belongs to user
        var validCommunityId = databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .ToList();

        var selectedCommunityId = communityId.HasValue && validCommunityId.Contains(communityId.Value)
            ? communityId.Value
            : validCommunityId.FirstOrDefault();

        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        var isCommunityAdmin = databaseContext.CommunityAdmins
            .Any(ca => ca.UserId == userId && ca.CommunityId == selectedCommunityId);

        var adminRole = user?.Role == "UnpaidAdmin";

        var posts = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == channelId)
            .OrderByDescending(post => post.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(post => new HomeViewModel.Post
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                Photo = post.Photo != null,
                IsAdmin = adminRole && isCommunityAdmin
            })
            .ToList();

        return PartialView("_PostsPartial", posts);
    }


    [HttpGet("image/{postId}")]
    public IActionResult GetImage(Guid postId)
    {
        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == postId);
        if (post?.Photo == null) return NotFound();
        //Response.Headers.CacheControl = "public,max-age=31536000";
        return File(post.Photo, "image/jpeg");
    }
    
    [HttpGet("community/image/{communityId}")]
    public IActionResult GetCommunityImage(Guid communityId)
    {
        var community = databaseContext.Communities.FirstOrDefault(c => c.Id == communityId);
        if (community?.Picture == null) return NotFound();
        Response.Headers.CacheControl = "public,max-age=31536000";
        return File(community.Picture, "image/jpeg");
    }
    
    [HttpGet("OpenedPost/{id}")]
    public IActionResult OpenedPost(Guid id)
    {
        var postDo = databaseContext.Posts
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == id);

        if (postDo == null)
        {
            return NotFound("Příspěvek nebyl nalezen.");
        }

        // Přemapování na ViewModel
        var postViewModel = new HomeViewModel.Post
        {
            Id = postDo.Id,
            Title = postDo.Title,
            Description = postDo.Description,
            CreatedBy = $"{postDo.User.Firstname} {postDo.User.LastName}" ?? "Neznámý",
            CreatedAt = postDo.CreatedAt,
            Photo = postDo.Photo != null // true pokud má fotku
        };

        return PartialView("_OpenedPost", postViewModel);
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("Error");
    }
}