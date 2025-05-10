using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Application.Infastructure.Database.Models.Enum;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers;

[Route("posts")]
public class PostsController(DatabaseContext databaseContext) : Controller
{
    [HttpPost]
    public IActionResult Create(CreatePostViewModel model)
    { // Server-side validation
        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError("Title", "Je potřeba napsat Předmět");
        }
        else if (model.Title.Length > 100)
        {
            ModelState.AddModelError("Title", "Předmět může mít maximálně 100 znaků");
        }

        if (string.IsNullOrWhiteSpace(model.Content))
        {
            ModelState.AddModelError("Content", "Je potřeba napsat do popisu");
        }
        else if (model.Content.Length > 1000)
        {
            ModelState.AddModelError("Content", "Popis může mít maximálně 1000 znaků");
        }

        if (!ModelState.IsValid)
        {
            return View("~/Views/Home/Index.cshtml", model);
        }

        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return RedirectToAction("Login", "Account");

        if (user.Role == "Banned")
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
        }

        // --- Use CommunityId from the form model
        var communityId = model.CommunityId;

        if (string.IsNullOrEmpty(communityId))
        {
            ModelState.AddModelError("", "Nebyla vybrána žádná komunita.");
            return View("~/Views/Home/Index.cshtml", model);
        }

        // --- Get the first channel for the selected community
        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId.ToString() == communityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        if (channelId == Guid.Empty)
        {
            ModelState.AddModelError("", "Nebyl nalezen žádný kanál pro vybranou komunitu.");
            return View("~/Views/Home/Index.cshtml", model);
        }

        byte[]? imageData = null;

        if (model.Photo != null && model.Photo.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            using (var image = Image.Load(model.Photo.OpenReadStream()))
            {
                var options = new ResizeOptions
                {
                    Size = new Size(800, 600),
                    Mode = ResizeMode.Max
                };
                image.Mutate(x => x.Resize(options));
                image.SaveAsJpeg(memoryStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = 80
                });
            }
            imageData = memoryStream.ToArray();
        }

        if (user != null)
        {
            var post = new PostDo
            {
                Id = default,
                Title = model.Title,
                Description = model.Content,
                CreatedAt = DateTimeOffset.Now,
                CreatedBy = user.Id,
                Type = model.Type,
                Place = "Zlín",
                User = user,
                Photo = imageData,
                ChannelId = channelId,
            };
            if (post.Type == "Petition")
            {
                post.Status = "Otevřená";
            }
            databaseContext.Posts.Add(post);
        }

        databaseContext.SaveChanges();
        return RedirectToAction("Index", "Home", new { communityId = model.CommunityId });

    }
}
