using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClippyWeb.Pages
{
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<AboutModel> _logger;

        public PrivacyModel(ILogger<AboutModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }

}
