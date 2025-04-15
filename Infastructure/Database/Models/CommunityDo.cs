namespace Application.Infastructure.Database.Models;

public class CommunityDo
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public byte[]? Picture { get; set; }
    public string? PostalCode { get; init; }

    public ICollection<UserCommunityDo> UserCommunities { get; init; } = [];
    public ICollection<CommunityAdminDo> AdminUsers { get; init; } = [];
    public ICollection<ChannelDo> Channels { get; init; } = [];
}
