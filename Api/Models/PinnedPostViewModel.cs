namespace Application.Api.Models;

public class PinnedPostViewModel
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public Guid CreatedById { get; set; }
    public bool UserHasPhoto { get; set; }
    public bool isAdmin { get; set; }
}