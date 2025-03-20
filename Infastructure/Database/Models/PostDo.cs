namespace Application.Infastructure.Database.Models;

public class PostDo
{
    public required Guid Id { get; set; }
    
    public required string Title { get; set; }
    
    public required string Description { get; set; }
    
    public required DateTimeOffset CreatedAt { get; set; }
    
    public required Guid CreatedBy { get; set; }
    
    public UserDo? CreatedByUser { get; set; }
}
