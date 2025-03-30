using Application.Api.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;


[Route("/delete")]
public class DeletePostController(DatabaseContext databaseContext) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await databaseContext.Posts.FindAsync(id);
    
        if (post == null)
        {
            return Json(new { success = false, message = "Příspěvek nenalezen." });
        }

        databaseContext.Posts.Remove(post);
        await databaseContext.SaveChangesAsync();

        return Json(new { success = true, message = "Příspěvek byl úspěšně odstraněn.", postId = id });
    }
}