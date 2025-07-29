using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Options;
using src.Options;

namespace src.Pages
{
    public class UserLookupModel : PageModel
    {
        [BindProperty]
        public string UPN { get; set; }

        public Dictionary<string, string> UserAttributes { get; set; }
        public GraphOptions GraphOptions { get; set; }
        public UserLookupModel(IOptions<GraphOptions> graphOptions)
        {
            GraphOptions = graphOptions.Value;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(UPN))
                return Page();

            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));

            try
            {
                var user = await graphClient.Users[UPN]
                    .GetAsync();

                UserAttributes = new Dictionary<string, string>
            {
                { "Display Name", user.DisplayName },
                { "Email", user.Mail },
                { "UPN", user.UserPrincipalName },
                { "Job Title", user.JobTitle },
                { "Department", user.Department }
            };
            }
            catch (ServiceException ex)
            {
                UserAttributes = new Dictionary<string, string>
            {
                { "Error", ex.Message }
            };
            }

            return Page();
        }
    }
}

