namespace Application.Api.Models;

public class EditPostViewModel
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    public IFormFile? Photo { get; set; }
    public bool RemoveImage { get; set; }
}