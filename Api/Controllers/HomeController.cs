using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;

[Authorize]
[Route("/")]
public class HomeController(DatabaseContext databaseContext) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return RedirectToAction("Login", "Account"); // Přesměrování na přihlášení

        var communityId = databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .FirstOrDefault();

        var communityName = databaseContext.Communities
            .Where(c => c.Id == communityId)
            .Select(c => c.Name)
            .FirstOrDefault();

        if (communityId == default)
            communityName = "No Community";

        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == communityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        var posts = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == channelId)
            .OrderByDescending(post => post.CreatedAt)
            .Select(post => new HomeViewModel.Post
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                Photo = post.Photo != null
            })
            .ToList();

        var homeViewModel = new HomeViewModel
        {
            Posts = posts,
            CommunityName = communityName
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


    [HttpGet("image/{postId}")]
    public IActionResult GetImage(Guid postId)
    {
        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == postId);
        if (post?.Photo == null) return NotFound();
        //Response.Headers.CacheControl = "public,max-age=31536000";
        return File(post.Photo, "image/jpeg");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("Error");
    }
}