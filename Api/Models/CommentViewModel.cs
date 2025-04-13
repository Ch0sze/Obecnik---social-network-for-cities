namespace Application.Api.Models;

public class CommentViewModel
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }

    public string UserFullName { get; set; } = string.Empty;
    //public string? UserPicture { get; set; }
}