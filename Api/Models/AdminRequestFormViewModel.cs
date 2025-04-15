using System.ComponentModel.DataAnnotations;
using Application.Infastructure.Database.Models;

namespace Application.Api.Models
{
    public class AdminRequestFormViewModel
    {
        public Guid Id { get; set; }
        public Guid CommunityId { get; set; }

        [EmailAddress(ErrorMessage = "Neplatná emailová adresa")]
        public string OfficialEmail { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Zadejte počet obyvatel")]
        [Range(1, int.MaxValue, ErrorMessage = "Počet obyvatel musí být větší než 0")]
        public int Population { get; set; }

        public DateTime RequestDate { get; set; }
        public CommunityDo? Community { get; set; }
        public UserDo? User { get; set; }
        public List<CommunityDo> Communities { get; set; } = new List<CommunityDo>();
    }
}