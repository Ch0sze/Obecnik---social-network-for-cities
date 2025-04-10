namespace Application.Infastructure.Database.Models;

public class UserDo
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Firstname { get; init; }

    public required string LastName { get; init; }

    public required byte[] PasswordHash { get; set; }

    public required byte[] PasswordSalt { get; set; }

    public required string Role { get; set; }

    public string? Residence { get; init; }
    public string? Description { get; init; }
    public string? PasswordLink { get; set; }
    public string? PostalCode { get; init; }
    public string? Picture { get; init; }

    public ICollection<UserCommunityDo> UserCommunities { get; init; } = [];
    public ICollection<PostDo> Posts { get; init; } = [];
    public ICollection<CommentDo> Comments { get; init; } = [];
}
