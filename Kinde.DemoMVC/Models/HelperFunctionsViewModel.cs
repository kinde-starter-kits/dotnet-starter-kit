using Kinde.Api.Models.User;

namespace Kinde.DemoMVC.Models
{
    public class HelperFunctionsViewModel
    {
        public KindeUserDetail? UserDetail { get; set; }
        public string? IssClaim { get; set; }
        public string? Organization { get; set; }
        public string? UserOrganizations { get; set; }
        public string? ThemeFlag { get; set; }
        public string? IsDarkMode { get; set; }
        public string? Theme { get; set; }
        public int? CompetitionsLimit { get; set; }
    }
}
