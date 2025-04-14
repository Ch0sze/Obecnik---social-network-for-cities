namespace Application.Api.Models;

public class AdminPaygateViewModel
{
    public string OfficialEmail { get; set; } = null!;
    public int TotalPopulation { get; set; }
    public decimal TotalAmount { get; set; }
}
