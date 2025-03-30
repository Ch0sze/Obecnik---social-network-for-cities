using Application.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;

[Authorize]  
[Route("/")]
public class HomeController(DatabaseContext databaseContext) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var userId = User.GetId(); // Získání ID přihlášeného uživatele
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return Unauthorized(); // Ošetření nepřihlášeného uživatele
        var communityId = databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .FirstOrDefault();

        if (communityId == default)
            return View(new HomeViewModel { Posts = new List<HomeViewModel.Post>() }); // Pokud uživatel nemá komunitu, vrátíme prázdný seznam

        // 2. Najdeme ChannelId této komunity
        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == communityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        if (channelId == default)
            return View(new HomeViewModel { Posts = new List<HomeViewModel.Post>() });
        
        var homeViewModel = new HomeViewModel
        {
            Posts = databaseContext.Posts
                .Include(post => post.User)
                .Where(post => post.ChannelId == channelId)
                .ToList()
                .OrderByDescending(post => post.CreatedAt)
                .Select(post => new HomeViewModel.Post
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    CreatedBy = string.Join(" ", post.User!.Firstname, post.User!.LastName),
                    Photo = post.Photo != null
                })
                .ToList(),
        };

        return View(homeViewModel);  
    }
    
    [HttpGet("image/{postId}")]
    public IActionResult GetImage(Guid postId)
    {
        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == postId);
        if (post?.Photo == null)
        {
            return NotFound();
        }
        Response.Headers.CacheControl = "public,max-age=31536000";
        return File(post.Photo, "image/jpeg");
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    { 
        return View("Error");
    }

}