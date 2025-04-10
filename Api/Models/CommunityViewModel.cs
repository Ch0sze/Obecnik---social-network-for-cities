
namespace Application.Api.Models
{
    public class CommunityViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; } = string.Empty;  // Make ImageUrl nullable
        public bool Dot { get; set; } = true;
        public string Link { get; set; } = "/#";
        
        public string Community { get; set; } = string.Empty;
    }
    
}