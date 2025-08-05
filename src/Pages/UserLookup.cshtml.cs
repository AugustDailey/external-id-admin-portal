using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Options;
using src.Options;
using Microsoft.Graph.Models;

namespace src.Pages
{
    public class UserLookupModel : PageModel
    {
        [BindProperty]
        public string UPN { get; set; }

        [BindProperty]
        public Dictionary<string, string> UserAttributes { get; set; }
        public GraphOptions GraphOptions { get; set; }
        public UserLookupModel(IOptions<GraphOptions> graphOptions)
        {
            GraphOptions = graphOptions.Value;
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(UPN))
                return Page();

            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));

            try
            {

                var user = await graphClient.Users[UPN].GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = new string[] { "displayName", "givenName", "identities", "extension_52104e8f53e04ca29658b024fba16661_userType" };
                });


                UserAttributes = new Dictionary<string, string>
                {
                    { "Display Name", user.DisplayName },
                    { "First Name", user.GivenName },
                    { "Last Name", user.Surname },
                    { "Username", user.Identities.First(x => x.SignInType == "emailAddress").IssuerAssignedId },
                    { "User Type", (string)user.AdditionalData["extension_52104e8f53e04ca29658b024fba16661_userType"] },
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

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));

            var userUpdate = new User
            {
                DisplayName = UserAttributes["Display Name"],
                GivenName = UserAttributes["First Name"],
                Surname = UserAttributes["Last Name"],
                Department = UserAttributes["Department"],
                AdditionalData = new Dictionary<string, object>
                {
                    { "extension_52104e8f53e04ca29658b024fba16661_userType", UserAttributes["User Type"] }
                }
            };

            try
            {
                await graphClient.Users[UPN].PatchAsync(userUpdate);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating user: {ex.Message}");
            }

            return await OnPostSearchAsync(); // Or return Page() to stay on the same page
        }

    }
}

