namespace Application.Api.Models;

public class CreateCommentViewModel
{
    public required string Content { get; set; }
    public required Guid PostId { get; set; }
}