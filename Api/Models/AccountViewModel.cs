namespace Application.Api.Models;
public record AccountViewModel
{
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hometown { get; set; } = string.Empty;
    
    public string? PostalCode { get; set; }
    
    public string Residence { get; set; } = string.Empty;
    
    public byte[]? Picture { get; set; } = null;
    public string? Role { get; set; } = string.Empty;
}