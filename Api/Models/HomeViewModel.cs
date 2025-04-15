using System.Threading.Channels;

namespace Application.Api.Models;

public class HomeViewModel
{
    public string? CommunityName { get; init; } = string.Empty;
    public required Guid CommunityId { get; set; } 
    public required List<Post> Posts { get; set; }

    public record Post
    {
        public string CommunityName { get; init; } = string.Empty;
        public required Guid Id { get; set; }

        public required string Title { get; set; }

        public required string Description { get; set; }

        public required DateTimeOffset CreatedAt { get; set; }

        public required string CreatedBy { get; set; }
        public bool Photo { get; set; }
        public bool IsAdmin { get; set; } 
        public bool IsPinned { get; set; }

        public string GetFormattedCreatedAt()
        {
            var now = DateTimeOffset.Now;
            var difference = now - CreatedAt;

            if (difference.TotalMinutes < 1)
                return "Právě teď";
            if (difference.TotalMinutes < 60)
                return $"Před {(int)difference.TotalMinutes} min";
            if (difference.TotalHours < 24)
                return $"Před {(int)difference.TotalHours} h";
            if (CreatedAt.Date == now.Date.AddDays(-1))
                return $"Včera v {CreatedAt:HH:mm}";
            
            if (CreatedAt.Hour < 12)
                return CreatedAt.ToString("dd.MM.yyyy v HH:mm");
                
            else return CreatedAt.ToString("dd.MM.yyyy ve HH:mm");
        }
    }
}