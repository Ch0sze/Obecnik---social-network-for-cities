using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Controllers;

[Route("editposts")]
public class EditPostController(DatabaseContext databaseContext) : Controller
{
    [HttpPost("{id}")]
    public IActionResult Edit(Guid id, EditPostViewModel model)
    {
        // Najdi příspěvek v databázi
        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == id);
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);
        
        if (user == null)
            return RedirectToAction("Login", "Account"); // Přesměrování na přihlášení
        
        if (post == null)
        {
            return Forbid();
        }
        // Validate the model
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Server-side validace
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
            return View("~/Views/Home/Edit.cshtml", model);
        }
        
        if (post.CreatedBy != userId)
        {
            return Forbid(); // Uživatel může upravovat pouze své vlastní příspěvky
        }
        
        var communityId = model.CommunityId;

        if (string.IsNullOrEmpty(communityId))
        {
            ModelState.AddModelError("", "Nebyla vybrána žádná komunita.");
            return View("~/Views/Home/Index.cshtml", model);
        }
        
        if (model.RemoveImage)
        {
            post.Photo = null;
        }
        // Handle new image upload
        else if (model.Photo != null && model.Photo.Length > 0)
        {
            using (var memoryStream = new MemoryStream())
            {
                // Načtení obrázku
                using (var image = Image.Load(model.Photo.OpenReadStream()))
                {
                    // Výpočet nových rozměrů se zachováním poměru stran
                    var options = new ResizeOptions
                    {
                        Size = new Size(800, 600),
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
                //aktualizace fotky
                post.Photo = memoryStream.ToArray();
            }
        }
        
        // Aktualizace příspěvku
        post.Title = model.Title;
        post.Description = model.Content;
        
        databaseContext.SaveChanges();
        
        return RedirectToAction("Index", "Home", new { communityId = model.CommunityId });
    }
}