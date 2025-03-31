namespace Application.Infastructure.Database.Models;

public class ChannelDo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public int? ForcedType { get; init; }

    public required Guid CommunityId { get; init; }
    public required CommunityDo Community { get; init; }

    public ICollection<PostDo> Posts { get; init; } = [];
}