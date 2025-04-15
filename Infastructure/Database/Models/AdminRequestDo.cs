using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Application.Infastructure.Database.Models;
public enum AdminRequestStatus
{
    Pending,
    Approved,
    Rejected
}
public class AdminRequestDo
{
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; init; }
    public UserDo User { get; init; } = null!;

    [Required]
    public Guid CommunityId { get; init; }
    public CommunityDo Community { get; init; } = null!;

    [Required]
    public string OfficialEmail { get; init; } = null!;

    public string? Notes { get; init; }

    [Required]
    public DateTime RequestDate { get; init; } = DateTime.UtcNow;

    [Required]
    public AdminRequestStatus Status { get; set; } = AdminRequestStatus.Pending;

    [Required]
    public int Population { get; set; }
}