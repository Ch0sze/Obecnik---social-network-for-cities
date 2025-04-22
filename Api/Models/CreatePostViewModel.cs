namespace Application.Api.Models;

public class CreatePostViewModel
{
    public required string Title { get; set; }
    public required string Content { get; set; }
    
    public required string Type{ get; set; }
    public IFormFile? Photo { get; set; }
    
    public string? CommunityId { get; set; }
}