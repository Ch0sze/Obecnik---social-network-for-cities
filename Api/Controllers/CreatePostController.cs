using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers;

[Route("posts")]
public class PostsController(DatabaseContext databaseContext) : Controller
{
    [HttpPost]
    public IActionResult Create(CreatePostViewModel model)
    {
        // Server-side validation
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
            // Return to the form with errors
            return View("~/Views/Home/Index.cshtml", model);
        }
        
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(user => user.Id == userId);
        if (user == null)
            return RedirectToAction("Login", "Account"); // Přesměrování na přihlášení

        // Check if user is banned
        if (user?.Role == "Banned")
        {
            // If banned, log them out and redirect to login with banned message
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
        }
        
        
        byte[]? imageData = null;
        var communityId = databaseContext.UserCommunities
            .Where(uc => user != null && uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .FirstOrDefault();

        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == communityId)
            .Select(c => c.Id)
            .FirstOrDefault();
        if (model.Photo != null && model.Photo.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Načtení obrázku
                using (var image = Image.Load(model.Photo.OpenReadStream()))
                {
                    // Výpočet nových rozměrů (zachová poměr stran)
                    var maxWidth = 800; // maximální šířka
                    var maxHeight = 600; // maximální výška
                
                    // Výpočet nových rozměrů se zachováním poměru stran
                    var options = new ResizeOptions
                    {
                        Size = new Size(maxWidth, maxHeight),
                        Mode = ResizeMode.Max
                    };
                
                    // Změna velikosti obrázku
                    image.Mutate(x => x.Resize(options));
                
                    // Uložení do formátu JPEG s 80% kvalitou
                    image.SaveAsJpeg(memoryStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                    {
                        Quality = 80
                    });
                }
            
                imageData = memoryStream.ToArray();
            }
        }

        if (user != null)
        {
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
                Photo = imageData,
                ChannelId = channelId,
            };

            databaseContext.Posts.Add(posts);
        }

        databaseContext.SaveChanges();

        return RedirectToAction("Index", "Home");
    }
}