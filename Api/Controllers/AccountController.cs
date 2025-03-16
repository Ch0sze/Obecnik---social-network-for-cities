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
            Name = user?.Name ?? string.Empty
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
            new(ClaimTypes.Name, user.Name),
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
        if (!ModelState.IsValid)
            return View("Register", model);

        var user = databaseContext.Users.FirstOrDefault(u => u.Email == model.Email);
        if (user != null)
        {
            ModelState.AddModelError("Email", "Tento e-mail je již použitý");
            return View("Register", model);
        }

        var (passwordSalt, passwordHash) = Password.Create(model.Password);
        var newUser = new UserDo
        {
            Id = default,
            Email = model.Email,
            Name = model.FirstName + " " + model.LastName,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            Role = string.Empty
        };

        await databaseContext.Users.AddAsync(newUser);
        await databaseContext.SaveChangesAsync();

        return View("RegisterSuccess");
    }

    [Authorize]
    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}