namespace Application.Infastructure.Database.Models;

public class CommunityAdminDo
{
    public Guid UserId { get; set; }
    public UserDo User { get; set; } = null!;

    public Guid CommunityId { get; set; }
    public CommunityDo Community { get; set; } = null!;
}