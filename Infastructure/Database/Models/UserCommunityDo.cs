namespace Application.Infastructure.Database.Models;

public class UserCommunityDo
{
    public required Guid UserId { get; init; }
    public required UserDo User { get; init; }
    
    public required Guid CommunityId { get; init; }
    public required CommunityDo Community { get; init; }
}