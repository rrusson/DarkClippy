using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Serilog;

namespace ClippyWeb.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly IConfiguration _configuration;

        public ErrorModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            // Add service URL and model information to the ViewData for display in the error page
            ViewData["ServiceUrl"] = _configuration["ServiceUrl"] ?? "Not configured";
            ViewData["ModelName"] = _configuration["Model"] ?? "Not configured";
            
            Log.Warning("Error page accessed. RequestId: {RequestId}", RequestId);
        }
    }
}
