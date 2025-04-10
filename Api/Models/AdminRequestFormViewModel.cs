using Application.Infastructure.Database.Models;

namespace Application.Api.Models;

public class AdminRequestFormViewModel
{
    public Guid Id { get; set; }
    public Guid CommunityId { get; set; }
    public string OfficialEmail { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime RequestDate { get; set; }
    public CommunityDo? Community { get; set; }
    public UserDo? User { get; set; }

}