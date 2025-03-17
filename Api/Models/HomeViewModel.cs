namespace Application.Api.Models;
public class HomeViewModel
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? Headline { get; set; }
    public string? Message { get; set; }
    public string? Date { get; set; }
    
    // Add the list of contributions
    // Initialize Contributions as an empty list or make it nullable
    public List<HomeViewModel> Contributions { get; set; } = new List<HomeViewModel>();  // Empty list by default
}