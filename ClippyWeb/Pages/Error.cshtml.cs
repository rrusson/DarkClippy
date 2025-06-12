using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClippyWeb.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;
        private readonly IConfiguration _configuration;

        public ErrorModel(ILogger<ErrorModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            // Add service URL and model information to the ViewData for display in the error page
            ViewData["ServiceUrl"] = _configuration["ServiceUrl"] ?? "Not configured";
            ViewData["ModelName"] = _configuration["Model"] ?? "Not configured";
            
            _logger.LogWarning($"Error page accessed. RequestId: {RequestId}");
        }
    }
}
