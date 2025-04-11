using System.Security.Claims;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Api.Services;
using Application.Core;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using CoatOfArmsDownloader.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;

[Route("account")]
public class AccountController(DatabaseContext databaseContext, IEmailService emailService) : Controller
{
    [HttpGet]
    [Authorize]
    public IActionResult Index()
    {
        var id = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(user => user.Id == id);

        // Check if user is found, otherwise redirect or show an error
        if (user == null)
        {
            return RedirectToAction("Error", "Home"); // or render a different error page
        }

        // Get the community data for the user
        var community = databaseContext.UserCommunities
            .Include(uc => uc.Community)
            .FirstOrDefault(uc => uc.UserId == user.Id)?.Community;

        var homeViewModel = new HomeViewModel
        {
            CommunityName = community?.Name ?? "Neznámá komunita", // Use community name
            Posts = new List<HomeViewModel.Post>(),
            CommunityId = community?.Id ?? Guid.Empty // Ensure the CommunityId is set correctly
        };

        // Pass both Account and HomeViewModel together as CombinedViewModel
        var combinedViewModel = new CombinedViewModel
        {
            AccountViewModel = new AccountViewModel
            {
                Email = user?.Email ?? string.Empty,
                Name = $"{user?.Firstname} {user?.LastName}",
                Hometown = $"{user?.Residence}, {user?.PostalCode}"
            },
            HomeViewModel = homeViewModel
        };

        return View("Account", combinedViewModel);
    }

    private string GetCommunityName(UserDo user)
    {
        var community = databaseContext.UserCommunities
            .Include(uc => uc.Community)
            .FirstOrDefault(uc => uc.UserId == user.Id)?.Community;

        return community?.Name ?? "Neznámá komunita"; // Fallback if no community is found
    }

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View("Login", new LoginViewModel
        {
            ReturnUrl = returnUrl
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

        if (Password.Verify(model.Password, user.PasswordHash, user.PasswordSalt) == false)
            return View("Login", model with { Message = "Nesprávné jméno nebo heslo." });

        var claims = new List<Claim>
        {
            new("Id", user.Id.ToString()),
            new(ClaimTypes.Name, string.Join(" ", user.Firstname, user.LastName)),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        Console.WriteLine($"User role: {user.Role}");
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
        return RedirectToAction("Index", "Home");
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

    private async Task AssignUserToCommunity(UserDo user)
    {
        var existingCommunity = await databaseContext.Communities
            .FirstOrDefaultAsync(c => c.PostalCode == user.PostalCode && c.Name == user.Residence);

        if (existingCommunity == null)
        {
            var newCommunity = new CommunityDo
            {
                Id = Guid.NewGuid(),
                Name = user.Residence ?? "Unknown Community",
                PostalCode = user.PostalCode,
                Picture = await CoatOfArmsScraper.GetCommunityCoatOfArms(user.Residence ?? "Unknown Community")
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

            var newCommunityMember = new UserCommunityDo
            {
                UserId = user.Id,
                CommunityId = newCommunity.Id,
                User = user,
                Community = newCommunity
            };

            await databaseContext.UserCommunities.AddAsync(newCommunityMember);
            await databaseContext.SaveChangesAsync();
        }
        else
        {
            var alreadyMember = await databaseContext.UserCommunities
                .AnyAsync(uc => uc.UserId == user.Id && uc.CommunityId == existingCommunity.Id);

            if (!alreadyMember)
            {
                var newCommunityMember = new UserCommunityDo
                {
                    UserId = user.Id,
                    CommunityId = existingCommunity.Id,
                    User = user,
                    Community = existingCommunity
                };

                await databaseContext.UserCommunities.AddAsync(newCommunityMember);
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


}