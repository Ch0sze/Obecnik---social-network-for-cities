namespace Application.Infastructure.Database.Models;

public class CommunityDo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Picture { get; init; }
    public string? PostalCode { get; init; }
    
    public ICollection<UserCommunityDo> UserCommunities { get; init; } = [];
    public ICollection<ChannelDo> Channels { get; init; } = [];
}