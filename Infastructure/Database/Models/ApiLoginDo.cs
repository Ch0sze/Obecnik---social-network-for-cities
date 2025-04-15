namespace Application.Infastructure.Database.Models;

public class ApiLoginDo
{
    public int Id { get; set; }
    public string NameHash { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}