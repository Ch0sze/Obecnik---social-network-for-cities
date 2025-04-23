namespace Application.Infastructure.Database.Models;

public class PetitionSignatureDo
{
    public required Guid UserId { get; init; }
    public required UserDo User { get; init; }

    public required Guid PostId { get; init; }
    public required PostDo Post { get; init; }

    public DateTimeOffset SignedAt { get; init; } = DateTimeOffset.UtcNow;
}