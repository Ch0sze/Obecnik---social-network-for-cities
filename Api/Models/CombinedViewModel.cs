namespace Application.Api.Models
{
    public class CombinedViewModel
    {
        public AccountViewModel? AccountViewModel { get; set; }
        public HomeViewModel? HomeViewModel { get; set; }
        public List<AdminRequestFormViewModel> AdminRequestViewModel { get; set; } = new List<AdminRequestFormViewModel>();
    }
}