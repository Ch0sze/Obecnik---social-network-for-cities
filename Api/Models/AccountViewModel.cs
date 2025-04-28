using System.ComponentModel.DataAnnotations;

namespace Application.Api.Models;
public record AccountViewModel
{

    public string Email { get; set; } = string.Empty;
    [Required(ErrorMessage = "Jméno nemůže být prázdné")]
    [MaxLength(256, ErrorMessage = "Jméno může mít maximálně 256 znaků")]
    [Display(Name = "Jméno")]
    public string FirstName { get; set; } = string.Empty;
    [Required(ErrorMessage = "Příjmení nemůže být prázdné")]
    [MaxLength(256, ErrorMessage = "Příjmení může mít maximálně 256 znaků")]
    [Display(Name = "Příjmení")]
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hometown { get; set; } = string.Empty;
    
    public string? PostalCode { get; set; }
    
    public string Residence { get; set; } = string.Empty;
    
    public byte[]? Picture { get; set; } = null;
    public string? Role { get; set; } = string.Empty;
}