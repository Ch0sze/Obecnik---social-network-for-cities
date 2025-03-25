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
        var homeViewModel = new HomeViewModel
        {
            Posts = databaseContext.Posts
                .Include(post => post.User)
                .ToList()
                .OrderByDescending(post => post.CreatedAt)
                .Select(post => new HomeViewModel.Post
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    CreatedBy = string.Join(" ", post.User!.Firstname, post.User!.LastName),
                })
                .ToList(),
        };

        return View(homeViewModel);  
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await databaseContext.Posts.FindAsync(id);
    
        if (post == null)
        {
            return NotFound();
        }

        databaseContext.Posts.Remove(post);
        await databaseContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    { 
        return View("Error");
    }

}