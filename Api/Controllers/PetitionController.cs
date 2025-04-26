using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using Application.Api.Extensions;
using OfficeOpenXml;            // EPPlus
using Microsoft.EntityFrameworkCore;

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
            /// <summary>
            /// Vygeneruje a stáhne skutečný Excel (.xlsx) se sloupci Jméno, Příjmení a prázdným Podpis.
            /// </summary>
            [HttpGet("{postId}/signatures/excel")]
            public IActionResult DownloadSignaturesExcel(Guid postId)
            {
                // EPPlus od verze 5 vyžaduje nastavit licenční kontext:
                ExcelPackage.License.SetNonCommercialOrganization("Obecník");



                // Načteme z DB jen to, co potřebujeme
                var sigs = databaseContext.PetitionSignatures
                    .Where(p => p.PostId == postId)
                    .OrderBy(p => p.SignedAt)
                    .Select(p => new {
                        FullName = p.User.Firstname + " " + p.User.LastName,
                        SignedAt = p.SignedAt
                    })
                    .ToList();

                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Podpisy");

                // 1) Hlavička
                ws.Cells[1, 1].Value = "Jméno";
                ws.Cells[1, 2].Value = "Příjmení";
                ws.Cells[1, 3].Value = "Podpis";  // záměrně prázdný sloupec

                // 2) Řádky s daty
                for (int i = 0; i < sigs.Count; i++)
                {
                    var row = i + 2;
                    var parts = sigs[i].FullName?.Split(' ') ?? Array.Empty<string>();
                    var first = parts.FirstOrDefault() ?? "";
                    var last  = parts.Length > 1 ? string.Join(" ", parts.Skip(1)) : "";

                    ws.Cells[row, 1].Value = first;
                    ws.Cells[row, 2].Value = last;
                    ws.Cells[row, 3].Value = "";  // Podpis necháme prázdný
                }

                // 3) Připravíme bytes a vrátíme jako soubor
                var fileBytes = package.GetAsByteArray();
                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Podpisy_petice.xlsx"
                );
            }
        }
    }
