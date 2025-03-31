using System.ComponentModel.DataAnnotations;

namespace Application.Api.Models;

public record ResetPasswordViewModel
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "Heslo nemůže být prázdné")]
    [DataType(DataType.Password)]
    [MaxLength(256, ErrorMessage = "Heslo může mít maximálně 256 znaků")]
    [MinLength(8, ErrorMessage = "Heslo musí obsahovat alespoň 8 znaků")]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Heslo musí obsahovat alespoň jedno velké písmeno a jedno číslo")]
    [Display(Name = "Heslo")]
    public string NewPassword { get; init; } = string.Empty;

    [Required(ErrorMessage = "Hesla se neshodují")]
    [DataType(DataType.Password)]
    [MaxLength(256, ErrorMessage = "Heslo může mít maximálně 256 znaků")]
    [Compare("NewPassword", ErrorMessage = "Hesla se neshodují")]
    [Display(Name = "Opakovat heslo")]
    public string ConfirmPassword { get; init; } = string.Empty;
    
    public string Message { get; init; } = string.Empty;

}