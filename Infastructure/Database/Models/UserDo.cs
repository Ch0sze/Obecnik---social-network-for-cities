namespace Application.Infastructure.Database.Models;

public class UserDo
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required string Firstname { get; init; }
    
    public required string LastName { get; init; }

    public required byte[] PasswordHash { get; init; }

    public required byte[] PasswordSalt { get; init; }

    public required string Role { get; init; }

    public ICollection<UserCommunityDo> UserCommunities { get; init; } = [];
    public ICollection<PostDo> Posts { get; init; } = [];
    public ICollection<CommentDo> Comments { get; init; } = [];
}