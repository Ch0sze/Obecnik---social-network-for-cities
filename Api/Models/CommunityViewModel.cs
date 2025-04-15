
namespace Application.Api.Models
{
    // Represents a community, usually for displaying on UI
    public class CommunityViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;
        public bool Dot { get; set; } = true;
        public string Link { get; set; } = string.Empty;
    }

    // Used specifically when joining a community (form model)
    public class JoinCommunityViewModel
    {
        public string Community { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}