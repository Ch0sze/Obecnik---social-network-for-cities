using System.ComponentModel.DataAnnotations;

namespace Application.Api.Models;

public record RegisterViewModel
{
    [Required(ErrorMessage = "Email nemůže být prázdný")]
    [EmailAddress(ErrorMessage = "Neplatná emailová adresa")]
    [MaxLength(512, ErrorMessage = "Email může mít maximálně 256 znaků")]
    [Display(Name = "Email")]
    public string Email { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Jméno nemůže být prázdné")]
    [MaxLength(256, ErrorMessage = "Jméno může mít maximálně 256 znaků")]
    [Display(Name = "Jméno")]
    public string FirstName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Příjmení nemůže být prázdné")]
    [MaxLength(256, ErrorMessage = "Příjmení může mít maximálně 256 znaků")]
    [Display(Name = "Příjmení")]
    public string LastName { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Heslo nemůže být prázdné")]
    [DataType(DataType.Password)]
    [MaxLength(256, ErrorMessage = "Heslo může mít maximálně 256 znaků")]
    [MinLength(8, ErrorMessage = "Heslo musí obsahovat alespoň 8 znaků")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "Heslo musí obsahovat alespoň jedno velké písmeno a jedno číslo")]
    [Display(Name = "Heslo")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Hesla se neshodují")]
    [DataType(DataType.Password)]
    [MaxLength(256, ErrorMessage = "Heslo může mít maximálně 256 znaků")]
    [Compare("Password", ErrorMessage = "Hesla se neshodují")]
    [Display(Name = "Opakovat heslo")]
    public string PasswordRepeat { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}