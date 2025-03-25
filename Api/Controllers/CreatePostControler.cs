using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers;

[Route("posts")]
public class PostsController(DatabaseContext databaseContext) : Controller
{
    [HttpPost]
    public IActionResult Create(CreatePostViewModel model)
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(user => user.Id == userId);
        if (user == null)
            throw new Exception("User is null");
        
        var posts = new PostDo
        {
            Id = default,
            Title = model.Title,
            Description = model.Content,
            CreatedAt = DateTimeOffset.Now,
            CreatedBy = user.Id,
            Type = "Discussion",
            Place = "Zlín",
            User = user,
            ChannelId = Guid.NewGuid(),
        };

        databaseContext.Posts.Add(posts);
        databaseContext.SaveChanges();

        return RedirectToAction("Index", "Home");
    }
}