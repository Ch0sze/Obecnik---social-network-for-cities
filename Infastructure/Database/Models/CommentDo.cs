namespace Application.Infastructure.Database.Models;

public class CommentDo
{
    public required Guid Id { get; init; }
    public required string Content { get; init; }
    public required DateTime DateTime { get; init; }

    public Guid? UserId { get; init; }
    public UserDo? User { get; init; }

    public required Guid PostId { get; init; }
    public required PostDo Post { get; init; }
}