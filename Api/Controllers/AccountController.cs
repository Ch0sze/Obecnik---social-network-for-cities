using System.Security.Claims;
using Application.Api.Extensions;
using Application.Api.Models;
using Application.Core;
using Application.Infastructure.Database;
using Application.Infastructure.Database.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Application.Api.Controllers;

[Route("account")]
public class AccountController(DatabaseContext databaseContext) : Controller
{
    [HttpGet]
    [Authorize]
    public IActionResult Index()
    {
        var id = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(user => user.Id == id);
        return View("Account", new AccountViewModel
        {
            Email = user?.Email ?? string.Empty,
            Name = user?.Firstname ?? string.Empty
        });
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
    public IActionResult ForgotPasswordSubmit(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("ForgotPassword", model);
        }

        // Hledáme uživatele v databázi
        var user = databaseContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user == null)
        {
            ModelState.AddModelError("Email", "Tento e-mail není registrován.");
            return View("ForgotPassword", model);
        }

        model = model with { Message = "Pokyny k obnovení hesla byly odeslány na váš e-mail." };
        return View("ForgotPassword", model);
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
            Console.WriteLine($"ModelState is invalid. Errors:");
            foreach (var error in ModelState)
            {
                foreach (var subError in error.Value.Errors)
                {
                    Console.WriteLine($" - {error.Key}: {subError.ErrorMessage}");
                }
            }
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
            Role = string.Empty
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
        .FirstOrDefaultAsync(c => c.PostalCode == user.PostalCode && c.Name == $"{user.Residence}");

    if (existingCommunity != null)
    {
        // Add user to the existing community via UserCommunityDo
        var communityMember = new UserCommunityDo
        {
            UserId = user.Id,
            CommunityId = existingCommunity.Id,
            User = user,
            Community = existingCommunity
        };

        await databaseContext.UserCommunities.AddAsync(communityMember);
        Console.WriteLine($"User {user.Email} added to community {existingCommunity.Name}");
    }
    else
    {
        // Create a new community
        var newCommunity = new CommunityDo
        {
            Id = Guid.NewGuid(),
            Name = $"{user.Residence}",
            PostalCode = user.PostalCode
        };

        await databaseContext.Communities.AddAsync(newCommunity);
        await databaseContext.SaveChangesAsync(); // Save to get the newCommunity.Id

        // Reload user from DB to ensure it's tracked
        var trackedUser = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);

        if (trackedUser == null)
        {
            Console.WriteLine($"User {user.Email} not found in DB.");
            return;
        }

        // Add user as the first member of the new community
        var newCommunityMember = new UserCommunityDo
        {
            UserId = trackedUser.Id,
            CommunityId = newCommunity.Id,
            User = trackedUser,
            Community = newCommunity
        };

        await databaseContext.UserCommunities.AddAsync(newCommunityMember);

        // Create a new "Příspěvky" channel for the new community
        var postsChannel = new ChannelDo
        {
            Id = Guid.NewGuid(),
            Name = "Příspěvky", // Channel name
            CommunityId = newCommunity.Id, // Link to the new community
            Community = newCommunity // Link the channel to the new community
        };

        // Add the channel to the community
        await databaseContext.Channels.AddAsync(postsChannel);
        await databaseContext.SaveChangesAsync(); // Save the channel to the database

        Console.WriteLine($"New community created: {newCommunity.Name} with channel 'Příspěvky'");
    }

    await databaseContext.SaveChangesAsync(); // Final save for the user-community relation
}


    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}