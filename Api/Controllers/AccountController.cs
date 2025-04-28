using System.Security.Claims;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Api.Services;
using Application.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using CoatOfArmsDownloader.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.DevTools.V133.Database;

namespace Application.Api.Controllers;

[Route("account")]
public class AccountController(DatabaseContext databaseContext, IEmailService emailService) : Controller
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Index()
    {
        var id = User.GetId();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == id);

        // Check if user is banned
         if (user?.Role == "Banned")
         {
             // If banned, log them out and redirect to login with banned message
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Váš účet byl zabanován." });
         }
         
         if (user?.Role == "Admin" && user.AdminRoleExpiresAt is DateTime expiry && expiry < DateTime.UtcNow)
         {
             user.Role = "UnpaidAdmin";
             await databaseContext.SaveChangesAsync();
         }

        // Check if user is found, otherwise redirect or show an error
        if (user == null)
        {
            return RedirectToAction("Error", "Home"); // or render a different error page
        }

        var community = await databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.Community)
            .FirstOrDefaultAsync();


        var homeViewModel = new HomeViewModel
        {
            CommunityName = community?.Name ?? "Neznámá komunita", // Use community name
            Posts = new List<HomeViewModel.Post>(),
            CommunityId = community?.Id ?? Guid.Empty // Ensure the CommunityId is set correctly
        };

        var accountViewModel = MapUserDoToAccountViewModel(user);
        // Pass both Account and HomeViewModel together as CombinedViewModel
        var combinedViewModel = new CombinedViewModel
        {

            AccountViewModel = accountViewModel,
            HomeViewModel = homeViewModel
        };

        return View("Account", combinedViewModel);
    }
    
    private AccountViewModel MapUserDoToAccountViewModel(UserDo user)
    {
        return new AccountViewModel
        {
            Email = user.Email,
            FirstName = user.Firstname,
            LastName = user.LastName,
            Name = $"{user?.Firstname} {user?.LastName}",
            Hometown = $"{user?.Residence}, {user?.PostalCode}",
            Residence = user!.Residence ?? string.Empty,
            PostalCode = user.PostalCode ?? string.Empty,
            Picture = user.Picture,
            Role = user.Role,
        };
    }


    private string GetCommunityName(UserDo user)
    {
        var community = databaseContext.UserCommunities
            .Include(uc => uc.Community)
            .FirstOrDefault(uc => uc.UserId == user.Id)?.Community;

        return community?.Name ?? "Neznámá komunita"; // Fallback if no community is found
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl, string? message)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View("Login", new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Message = message
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginSubmit(LoginViewModel model)
    {
        await UpdateMissingCommunityPictures();

        if (!ModelState.IsValid)
            return View("Login", model);

        var user = databaseContext.Users.FirstOrDefault(user => user.Email == model.Email);
        if (user == null)
            return View("Login", model with { Message = "Nesprávné jméno nebo heslo." });
        
        if (user.Role == "Banned")
            return View("Login", model with { Message = "Váš účet byl zabanován." });

        if (!Password.Verify(model.Password, user.PasswordHash, user.PasswordSalt))
            return View("Login", model with { Message = "Nesprávné jméno nebo heslo." });

        var claims = new List<Claim>
        {
            new("Id", user.Id.ToString()),
            new(ClaimTypes.Name, $"{user.Firstname} {user.LastName}"),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            AllowRefresh = true,
            IsPersistent = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return string.IsNullOrEmpty(model.ReturnUrl)
            ? RedirectToAction("Index", "Home")
            : Redirect(model.ReturnUrl);
    }


    [HttpGet("forgotpassword")]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View("ForgotPassword", new ForgotPasswordViewModel());
    }

    [HttpPost("forgotpasswordsubmit")]
    public async Task<IActionResult> ForgotPasswordSubmit(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View("ForgotPassword", model);

        // Hledáme uživatele podle e-mailu
        var user = databaseContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError("Email", "Tento e-mail není registrován.");
            return View("ForgotPassword", model);
        }

        // Vygenerujeme resetovací token s expirací (30 minut)
        var token = $"{Guid.NewGuid()}_{DateTime.UtcNow.AddMinutes(30).Ticks}";
        user.PasswordLink = token;
        await databaseContext.SaveChangesAsync();

        // Vytvoříme URL pro reset hesla
        var callbackUrl = Url.Action("ResetPassword", "Account",
            new { token, email = user.Email },
            Request.Scheme);

        if (string.IsNullOrEmpty(callbackUrl)) throw new Exception("Nepodařilo se vygenerovat URL pro reset hesla.");

        // Použijeme injektovanou instanci emailové služby (nikoliv třídu EmailService)
        await emailService.SendResetEmail(user.Email, callbackUrl);

        // Aktualizujeme ViewModel s informací pro uživatele
        model = model with { Message = "Pokyny k obnovení hesla byly odeslány na váš e-mail." };
        return View("ForgotPassword", model);
    }


    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }


    [HttpGet("resetpassword")]
    public IActionResult ResetPassword(string token, string email)
    {
        // Pokud chybí token nebo e-mail, zobrazíme chybovou stránku.
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            return View("ResetPasswordError", new ErrorViewModel { Message = "Neplatný odkaz." });
        }

        // Najdeme uživatele podle e-mailu
        var user = databaseContext.Users.FirstOrDefault(u => u.Email == email);
        if (user == null || string.IsNullOrEmpty(user.PasswordLink) || user.PasswordLink != token)
        {
            return View("ResetPasswordError", new ErrorViewModel { Message = "Odkaz je neplatný nebo již použit." });
        }

        // Rozdělíme token na dvě části očekávaného formátu "GUID_TICKS"
        var parts = token.Split('_');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var expirationTicks))
        {
            return View("ResetPasswordError", new ErrorViewModel { Message = "Odkaz je poškozený." });
        }

        var expirationTime = new DateTime(expirationTicks, DateTimeKind.Utc);
        if (DateTime.UtcNow > expirationTime)
        {
            return View("ResetPasswordError", new ErrorViewModel { Message = "Odkaz vypršel." });
        }

        // Token je platný – vytvoříme view model a zobrazíme formulář pro reset hesla
        var model = new ResetPasswordViewModel
        {
            Token = token,
            Email = email
        };

        return View("ResetPassword", model);
    }



    [HttpPost("resetpasswordsubmit")]
    public async Task<IActionResult> ResetPasswordSubmit(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View("ResetPassword", model);

        // Najdeme uživatele podle e-mailu
        var user = databaseContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordLink))
        {
            ModelState.AddModelError("", "Neplatný požadavek.");
            return View("ResetPassword", model);
        }

        // Ověříme, zda token z URL sedí s tokenem uloženým v DB
        if (user.PasswordLink != model.Token)
        {
            ModelState.AddModelError("", "Neplatný nebo již použitý odkaz.");
            return View("ResetPassword", model);
        }

        // Ověříme formát tokenu, očekáváme "GUID_TICKS"
        var parts = model.Token.Split('_');
        if (parts.Length != 2 || !long.TryParse(parts[1], out var expirationTicks))
        {
            ModelState.AddModelError("", "Neplatný token.");
            return View("ResetPassword", model);
        }

        var expirationTime = new DateTime(expirationTicks, DateTimeKind.Utc);
        if (DateTime.UtcNow > expirationTime)
        {
            ModelState.AddModelError("", "Odkaz vypršel.");
            return View("ResetPassword", model);
        }

        var (newSalt, newHash) = Password.Create(model.NewPassword);
        user.PasswordSalt = newSalt;
        user.PasswordHash = newHash;
        user.PasswordLink = null;

        await databaseContext.SaveChangesAsync();

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        return RedirectToAction("ResetPasswordConfirmation");
    }


    [HttpGet("resetpasswordconfirmation")]
    public IActionResult ResetPasswordConfirmation()
    {
        return View("ResetPasswordConfirmation");
    }


    [HttpGet("register")]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View("Register", new RegisterViewModel());
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterSubmit(RegisterViewModel model)
    {
        Console.WriteLine($"Registering user: {model.Email}");
        if (!ModelState.IsValid)
        {
            Console.WriteLine("ModelState is invalid. Errors:");
            foreach (var error in ModelState)
            foreach (var subError in error.Value.Errors)
                Console.WriteLine($" - {error.Key}: {subError.ErrorMessage}");

            return View("Register", model);
        }


        // Log the model data to confirm it's being received correctly
        Console.WriteLine($"Registering user: {model.Email}");

        var user = databaseContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user != null)
        {
            ModelState.AddModelError("Email", "Tento e-mail je již použitý");
            return View("Register", model);
        }

        var (passwordSalt, passwordHash) = Password.Create(model.Password);
        var newUser = new UserDo
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            Firstname = model.FirstName,
            LastName = model.LastName,
            Residence = model.Residence,
            PostalCode = model.PostalCode,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = model.Email == "obecnika@gmail.com" ? "SuperAdmin" : "User"
        };

        await databaseContext.Users.AddAsync(newUser);
        await databaseContext.SaveChangesAsync();

        // Assign the user to a community
        await AssignUserToCommunity(newUser);


        return View("RegisterSuccess");
    }

    private async Task AssignUserToCommunity(UserDo? user)
    {
        var existingCommunity = await databaseContext.Communities
            .FirstOrDefaultAsync(c => user != null && c.PostalCode == user.PostalCode && c.Name == user.Residence);

        if (existingCommunity == null)
        {
            var newCommunity = new CommunityDo
            {
                Id = Guid.NewGuid(),
                Name = user?.Residence ?? "Unknown Community",
                PostalCode = user?.PostalCode,
                Picture = await CoatOfArmsScraper.GetCommunityCoatOfArms(user?.Residence ?? "Unknown Community")
            };

            await databaseContext.Communities.AddAsync(newCommunity);
            await databaseContext.SaveChangesAsync();

            var newChannel = new ChannelDo
            {
                Id = Guid.NewGuid(),
                Name = "Příspěvky",
                CommunityId = newCommunity.Id,
                Community = newCommunity
            };

            await databaseContext.Channels.AddAsync(newChannel);
            await databaseContext.SaveChangesAsync();

            if (user != null)
            {
                var newCommunityMember = new UserCommunityDo
                {
                    UserId = user.Id,
                    CommunityId = newCommunity.Id,
                    User = user,
                    Community = newCommunity
                };

                await databaseContext.UserCommunities.AddAsync(newCommunityMember);
            }

            await databaseContext.SaveChangesAsync();
        }
        else
        {
            var alreadyMember = await databaseContext.UserCommunities
                .AnyAsync(uc => user != null && uc.UserId == user.Id && uc.CommunityId == existingCommunity.Id);

            if (!alreadyMember)
            {
                if (user != null)
                {
                    var newCommunityMember = new UserCommunityDo
                    {
                        UserId = user.Id,
                        CommunityId = existingCommunity.Id,
                        User = user,
                        Community = existingCommunity
                    };

                    await databaseContext.UserCommunities.AddAsync(newCommunityMember);
                }

                await databaseContext.SaveChangesAsync();
            }
        }
    }

    public async Task<IActionResult> UpdateMissingCommunityPictures()
    {
        var communitiesWithoutPictures = await databaseContext.Communities
            .Where(c => c.Picture == null || c.Picture.Length == 0)
            .ToListAsync();

        foreach (var community in communitiesWithoutPictures)
        {
            try
            {
                var pictureBytes = await CoatOfArmsScraper.GetCommunityCoatOfArms(community.Name);
                if (pictureBytes != null && pictureBytes.Length > 0)
                {
                    community.Picture = pictureBytes;
                    Console.WriteLine($"Updated picture for: {community.Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update picture for {community.Name}: {ex.Message}");
            }
        }

        await databaseContext.SaveChangesAsync();

        return Ok($"Updated {communitiesWithoutPictures.Count} community pictures.");
    }
    
    [HttpGet("requestadminrights")]
    [Authorize]
    public IActionResult RequestAdminRights()
    {
        // Fetch communities from the database
        var communities = databaseContext.Communities.ToList();

        // Create the view model and populate the Communities list
        var model = new AdminRequestFormViewModel
        {
            Communities = communities // Add communities to the view model
        };

        return View("RequestAdminRights", model);
    }
    
    [HttpGet("adminpaygate")]
    [Authorize]
    public async Task<IActionResult> AdminPaygate()
    {
        var userId = User.GetId();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.Role != "UnpaidAdmin")
        {
            return RedirectToAction("Index", "Home"); // Only unpaid admins should see this
        }

        // Find approved admin requests by this user
        var approvedRequests = await databaseContext.AdminRequests
            .Where(r => r.OfficialEmail == user.Email && r.Status == AdminRequestStatus.Approved)
            .ToListAsync();

        // Calculate the total population of the approved requests
        var totalPopulation = approvedRequests.Sum(r => r.Population);

        // Create a view model for the paygate view
        var model = new AdminPaygateViewModel
        {
            OfficialEmail = user.Email,
            TotalPopulation = totalPopulation,
            TotalAmount = totalPopulation * 1.0m // e.g., 1 Kč per citizen
        };

        return View("AdminPaygate", model);
    } 
        
    [HttpPost("UpdateAccount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccount(CombinedViewModel model, IFormFile? ImageFile)
    {    
        // Ověření, zda model obsahuje data
        if (model == null || model.AccountViewModel == null)
        {
            ModelState.AddModelError("", "Data z formuláře nebyla přijata.");
            return View("Account", model);
        }

        // Pokud model není validní, načtěte aktuální obrázek z databáze,
        // aby se zobrazil i při opětovném vykreslení formuláře.
        if (!ModelState.IsValid)
        {
            var userId = User.GetId();
            var user = await databaseContext.Users.FindAsync(userId);
            if (user != null)
            {
                model.AccountViewModel.Picture = user.Picture;
            }
            return View("Account", model);
        }

        // Získání ID aktuálního uživatele z claims (přes vaši extension metodu GetId)
        var currentUserId = User.GetId();

        // Načtení uživatele z databáze
        var userFromDb = await databaseContext.Users.FindAsync(currentUserId);
        if (userFromDb == null)
        {
            return NotFound("Uživatel nebyl nalezen.");
        }

        // Aktualizace údajů uživatele
        userFromDb.Firstname = model.AccountViewModel.FirstName;
        userFromDb.LastName = model.AccountViewModel.LastName;
        userFromDb.Residence = model.AccountViewModel.Residence;
        userFromDb.PostalCode = model.AccountViewModel.PostalCode;
        
        // Zpracování změny obrázku
        byte[]? imageData = null;
        if (Request.Form["RemoveImage"] == "true")
        {
            userFromDb.Picture = null;
        }
        else if (ImageFile != null && ImageFile.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            using (var image = Image.Load(ImageFile.OpenReadStream()))
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
            userFromDb.Picture = imageData;
        }

        // Uložení změn do databáze
        await databaseContext.SaveChangesAsync();

        // Přesměrování po úspěšném uložení - obrázek na stránce zůstane, protože nedochází k reloadu současného náhledu
        return RedirectToAction("Index", "Account");
    }
    
    [HttpGet("users/{id}/picture")]
    public async Task<IActionResult> GetUserPicture(Guid id)
    {
        var user = await databaseContext.Users.FindAsync(id);
        if (user?.Picture == null || user.Picture.Length == 0)
        {
            return PhysicalFile("wwwroot/Images/GenericAvatar.png", "image/png");
        }

        return File(user.Picture, "image/jpeg");
    }
    /*[HttpGet("users/{id}/username")]
    public async Task<IActionResult> GetUsername(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("User ID is empty.");
        }

        var user = await databaseContext.Users.FindAsync(id);
    
        if (user == null)
        {
            return NotFound();
        }

        // Combine FirstName and LastName from the user model
        var fullName = $"{user.Firstname} {user.LastName}".Trim();
    
        // Log the fullName to verify it's being generated correctly
        Console.WriteLine($"Full name generated: {fullName}");

        // Return the full name as part of the response
        return Ok(new { username = fullName });
    }*/
    [HttpGet("users/{id}/Firstname")]
    public async Task<IActionResult> GetFullName(Guid id)
    {
        var user = await databaseContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var fullName = $"{user.Firstname} {user.LastName}";
        return Ok(new { username = fullName });
    }
    

}