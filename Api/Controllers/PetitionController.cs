using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Application.Api.Extensions;

namespace Application.Api.Controllers
{
    [Route("petitions")]
    public class PetitionController(DatabaseContext databaseContext) : Controller
    {
        // Podepsání petice
        [HttpPost("{postId}/sign")]
        public IActionResult SignPetition(Guid postId)
        {
            var userId = User.GetId();
            if (userId == null)
            {
                return BadRequest("Uživatel není přihlášen.");
            }

            // Kontrola, zda uživatel již tuto petici podepsal
            var alreadySigned = databaseContext.PetitionSignatures
                .Any(p => p.PostId == postId && p.UserId == userId);

            if (alreadySigned)
                return BadRequest("Už jste tuto petici podepsal.");

            var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);
            var post = databaseContext.Posts.FirstOrDefault(p => p.Id == postId);

            // Pokud uživatel nebo petice neexistují
            if (user == null || post == null)
                return NotFound();

            // Přidání podpisu do databáze
            var signature = new PetitionSignatureDo
            {
                PostId = postId,
                UserId = userId.Value,
                SignedAt = DateTimeOffset.UtcNow,
                User = user,
                Post = post
            };

            databaseContext.PetitionSignatures.Add(signature);
            databaseContext.SaveChanges();

            return Ok();
        }

        // Odstranění podpisu
        [HttpPost("{postId}/unsign")]
        public IActionResult UnsignPetition(Guid postId)
        {
            var userId = User.GetId();

            // Hledání existujícího podpisu
            var signature = databaseContext.PetitionSignatures
                .FirstOrDefault(p => p.PostId == postId && p.UserId == userId);

            if (signature == null)
                return NotFound();

            // Odebrání podpisu z databáze
            databaseContext.PetitionSignatures.Remove(signature);
            databaseContext.SaveChanges();

            return Ok();
        }

        // Získání počtu podpisů petice
        [HttpGet("{postId}/signatures/count")]
        public IActionResult GetSignatureCount(Guid postId)
        {
            var count = databaseContext.PetitionSignatures
                .Count(p => p.PostId == postId);

            return Ok(count);
        }

        // Zjištění, zda uživatel již podepsal
        [HttpGet("{postId}/signatures/me")]
        public IActionResult HasUserSigned(Guid postId)
        {
            var userId = User.GetId();

            var hasSigned = databaseContext.PetitionSignatures
                .Any(p => p.PostId == postId && p.UserId == userId);

            return Ok(hasSigned);
        }
        
        [HttpGet("{postId}/signatures")]
        public IActionResult GetSignatures(Guid postId)
        {
            var signatures = databaseContext.PetitionSignatures
                .Where(p => p.PostId == postId)
                .OrderByDescending(p => p.SignedAt)
                .Select(p => new
                {
                    FullName = p.User.Firstname + " " + p.User.LastName,
                    UserId = p.User.Id,
                    SignedAt = p.SignedAt
                })
                .ToList();

            return Ok(signatures);
        }

    }
}
