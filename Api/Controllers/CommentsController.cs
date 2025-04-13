using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;
using Application.Api.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Application.Api.Controllers;

[Route("Comments")]
public class CommentsController(DatabaseContext databaseContext) : Controller
{
    [HttpPost("Create")]
    public IActionResult Create(CreateCommentViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Content) || model.Content.Length > 500)
        {
            ModelState.AddModelError("Content", "Komentář nesmí být prázdný a musí mít max. 500 znaků.");
            return BadRequest(ModelState);
        }

        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (user.Role == "Banned")
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
        }

        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == model.PostId);
        if (post == null)
        {
            return NotFound("Příspěvek nenalezen.");
        }

        var comment = new CommentDo
        {
            Id = Guid.NewGuid(),
            Content = model.Content,
            DateTime = DateTime.Now,
            UserId = user.Id,
            PostId = model.PostId,
            Post = post
        };

        databaseContext.Comments.Add(comment);
        databaseContext.SaveChanges();

        return RedirectToAction("Index", "Home"); // Nebo zpět na detail postu
    }
    [HttpGet("{postId:guid}")]
    public IActionResult GetComments(Guid postId)
    {
        var comments = databaseContext.Comments
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.DateTime)
            .Select(c => new CommentViewModel
            {
                Id = c.Id,
                Content = c.Content,
                DateTime = c.DateTime,
                UserFullName = c.User != null ? $"{c.User.Firstname} {c.User.LastName}" : "Anonym",
                //UserPicture = c.User?.Picture
            })
            .ToList();

        return Ok(comments);
    }
}
