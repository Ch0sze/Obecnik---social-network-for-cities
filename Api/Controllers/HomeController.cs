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
                .Include(post => post.CreatedByUser)
                .ToList()
                .Select(post => new HomeViewModel.Post
                {
                    Id = post.Id,
                    Title = post.Title,
                    Description = post.Description,
                    CreatedAt = post.CreatedAt,
                    CreatedBy = string.Join(" ", post.CreatedByUser!.Firstname, post.CreatedByUser!.LastName),
                })
                .ToList(),
        };

        return View(homeViewModel);  
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    { 
        return View("Error");
    }

}