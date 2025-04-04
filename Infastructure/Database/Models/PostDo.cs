namespace Application.Infastructure.Database.Models;

public class PostDo
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Title { get; set; }
    public required string Type { get; init; }
    public required string Description { get; set; }
    public DateTime? EventDate { get; init; }
    public byte[]? Photo { get; set; }
    public required string Place { get; init; }
    public int? SignaturesNo { get; init; }

    public required Guid CreatedBy { get; init; }
    public required UserDo User { get; init; }

    public Guid? ChannelId { get; init; } //to be added

    public ChannelDo? Channel { get; init; } //to be added

    public ICollection<CommentDo> Comments { get; init; } = [];
}