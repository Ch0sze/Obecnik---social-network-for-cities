namespace Application.Infastructure.Database.Models;

public class UserDo
{
    public required Guid Id { get; init; }
    public required string Email { get; set; }
    public required string Firstname { get; set; }
    public required string LastName { get; set; }
    public required byte[] PasswordHash { get; set; }
    public required byte[] PasswordSalt { get; set; }
    public required string Role { get; set; }
    public string? Residence { get; set; }
    public string? Description { get; init; }
    public string? PasswordLink { get; set; }
    public string? PostalCode { get; set; }
    public string? Picture { get; set; }
    public DateTime? AdminRoleExpiresAt { get; set; }

    public ICollection<UserCommunityDo> UserCommunities { get; init; } = [];
    public ICollection<CommunityAdminDo> AdminCommunities { get; init; } = [];
    public ICollection<PostDo> Posts { get; init; } = [];
    public ICollection<CommentDo> Comments { get; init; } = [];
}

