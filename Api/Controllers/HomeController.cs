using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using Application.Api.Models;

namespace Application.Api.Controllers
{
    [Authorize]  // Keep the authorization in place
    public class HomeController : Controller
    {
        [HttpPost]
        public IActionResult CreatePost(string postTitle, string postContent, string location, DateTime? eventDate)
        {
            // Create a new post model
            var newPost = new HomeViewModel
            {
                Id = new Random().Next(100, 999), // You can update this to a better ID generation method
                UserName = User.Identity?.Name,
                Headline = postTitle,
                Message = postContent,
                Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
            };

            // Optionally, you could save this data in a session or a database
            // SaveContributions(newPost); // Method to save contributions

            // Return the new post as JSON for the front-end
            return Json(newPost);
        }
        // [HttpPost]
        // public IActionResult CreatePost(string postTitle, string postContent, string location, DateTime? eventDate)
        // {
        //     // Create a new post model
        //     var newPost = new HomeViewModel
        //     {
        //         Id = new Random().Next(100, 999), // NEEED TO REWRITE THIS LATER PICO
        //         UserName = User.Identity?.Name,
        //         Headline = postTitle,
        //         Message = postContent,
        //         Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm")
        //     };
        //
        //     // Add new post to the list of contributions
        //     var contributions = GetContributions(); // A method to retrieve the current contributions
        //     contributions.Insert(0, newPost); // Insert the new post at the top of the list
        //
        //     // Return the updated list as a partial view
        //     return PartialView("_Contributions", contributions);
        // }

        // Index action, serves the main page
        public IActionResult Index()
        {
            var contributions = GetContributions();  // Fetch the contributions from wherever you're storing them

            var homeViewModel = new HomeViewModel
            {
                Contributions = contributions
            };

            // Return the Index view with the contributions data
            return View(homeViewModel);  // This passes the data to Index.cshtml
        }

        // Error action, serves the error view
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        { 
            return View("Error");
        }

        // Helper method to get contributions (example, could be a database or session-based)
        private List<HomeViewModel> GetContributions()
        {
            // This is where you get the actual contributions from a database, session, or other data source
            return new List<HomeViewModel>
            {
                new HomeViewModel 
                { 
                    Id = 1, 
                    UserName = "Jan Novák", 
                    Headline = "Problém s parkováním na náměstí", 
                    Message = "Dobrý den, chtěl bych upozornit na problém s parkováním na hlavním náměstí. Každý den ráno je zde takový chaos, že se nedá bezpečně projít. Je možné nějak regulovat počet aut nebo vytvořit více parkovacích míst?", 
                    Date = DateTime.Now.AddMinutes(-10).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 2, 
                    UserName = "Petr Svoboda", 
                    Headline = "Nedostatek odpadkových košů", 
                    Message = "Ahoj všichni, všiml jsem si, že v našem parku chybí odpadkové koše. Lidé tak odhazují odpadky na zem, což vede k nepořádku. Mohli bychom společně požádat město o instalaci více košů?", 
                    Date = DateTime.Now.AddMinutes(-20).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 3, 
                    UserName = "Anna Dvořáková", 
                    Headline = "Hluk z nočních podniků", 
                    Message = "Chtěla bych se podělit o svůj problém s hlukem z nočních podniků v centru. Bydlím nedaleko a každý víkend je to opravdu nesnesitelné. Je možné nějak omezit hlasitou hudbu po určité hodině?", 
                    Date = DateTime.Now.AddMinutes(-30).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 4, 
                    UserName = "Eva Horáková", 
                    Headline = "Oprava chodníku", 
                    Message = "Dobrý den, chtěla bych upozornit na rozbitý chodník v ulici U Lesa. Je to nebezpečné, zejména pro starší občany a maminky s kočárky. Mohli bychom požádat o jeho opravu?", 
                    Date = DateTime.Now.AddMinutes(-40).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 5, 
                    UserName = "Marek Veselý", 
                    Headline = "Špatné osvětlení ve večerních hodinách", 
                    Message = "Ahoj, všiml jsem si, že v ulici Na Vyhlídce je večer velmi špatné osvětlení. Je to nebezpečné pro chodce i řidiče. Mohli bychom požádat o výměnu lamp nebo přidání nových?", 
                    Date = DateTime.Now.AddMinutes(-50).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 6, 
                    UserName = "Jana Králová", 
                    Headline = "Problém s veřejnou dopravou", 
                    Message = "Dobrý den, chtěla bych se zeptat, zda je možné zvýšit frekvenci autobusů v ranních hodinách. Každý den je přeplněný autobus a někteří lidé se ani nemohou dostat do práce včas.", 
                    Date = DateTime.Now.AddMinutes(-60).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 7, 
                    UserName = "Tomáš Malý", 
                    Headline = "Zanedbané dětské hřiště", 
                    Message = "Ahoj, všiml jsem si, že dětské hřiště v parku je ve špatném stavu. Houpačky jsou rozbité a pískoviště je plné nečistot. Mohli bychom to nějak řešit?", 
                    Date = DateTime.Now.AddMinutes(-70).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 8, 
                    UserName = "Lucie Černá", 
                    Headline = "Problém s cyklostezkou", 
                    Message = "Dobrý den, chtěla bych upozornit na nebezpečný úsek cyklostezky podél řeky. Povrch je rozbitý a hrozí zde úrazy. Je možné to nějak opravit?", 
                    Date = DateTime.Now.AddMinutes(-80).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 9, 
                    UserName = "Pavel Havel", 
                    Headline = "Nedostatek zeleně v centru", 
                    Message = "Ahoj všichni, všiml jsem si, že v centru města ubývá zeleně. Stromy jsou káceny a nové se nesází. Mohli bychom požádat o výsadbu nových stromů a keřů?", 
                    Date = DateTime.Now.AddMinutes(-90).ToString("dd.MM.yyyy HH:mm") 
                },
                new HomeViewModel 
                { 
                    Id = 10, 
                    UserName = "Veronika Šimková", 
                    Headline = "Problém s hlukem ze staveniště", 
                    Message = "Dobrý den, chtěla bych se podělit o svůj problém s hlukem z nedalekého staveniště. Práce začínají velmi brzy ráno a ruší to celou ulici. Je možné nějak omezit pracovní dobu?", 
                    Date = DateTime.Now.AddMinutes(-100).ToString("dd.MM.yyyy HH:mm") 
                }
            };
        }
    }
}
