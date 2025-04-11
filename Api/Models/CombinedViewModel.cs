using Application.Infastructure.Database.Models;

namespace Application.Api.Models
{
    public class CombinedViewModel
    {
        public AccountViewModel? AccountViewModel { get; set; }
        public HomeViewModel? HomeViewModel { get; set; }
        
        public List<UserDo>? Users { get; set; }
        public string? SearchQuery { get; set; }

        public List<AdminRequestFormViewModel> AdminRequestViewModel { get; set; } = new List<AdminRequestFormViewModel>();
    }
}