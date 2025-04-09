namespace Application.Api.Models
{
    public class CombinedViewModel
    {
        public AccountViewModel? AccountViewModel { get; set; }
        public HomeViewModel? HomeViewModel { get; set; }
        public AdminRequestFormViewModel AdminRequestViewModel { get; set; } = new AdminRequestFormViewModel();
    }
}