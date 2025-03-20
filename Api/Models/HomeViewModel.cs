namespace Application.Api.Models;

public class HomeViewModel
{
    public required List<Post> Posts { get; set; }

    public record Post
    {
        public required Guid Id { get; set; }
    
        public required string Title { get; set; }
    
        public required string Description { get; set; }
    
        public required DateTimeOffset CreatedAt { get; set; }
    
        public required string CreatedBy { get; set; }
    }
}