using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Options;
using src.Options;
using Microsoft.Graph.Models;
using src.Services;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace src.Pages
{
    public class UserManagementModel : PageModel
    {
        [BindProperty]
        public Dictionary<string, string> UserAttributes { get; set; }
        [BindProperty]
        public string SelectedAttribute { get; set; }
        [BindProperty]
        public string SearchValue { get; set; }
        [BindProperty]
        public List<SelectListItem> SearchableAttributes {get;set;}
        public GraphOptions GraphOptions { get; set; }
        public IGraphUserConfigService GraphUserConfigService { get; set; }

        public Dictionary<string,string> AllAttributes
        {
            get
            {
                if (_allAttributes == null)
                {
                    _allAttributes = new Dictionary<string,string>();
                    foreach(var attr in _defaultAttributes)
                    {
                        _allAttributes.Add(attr.Key, attr.Value);
                    }
                    foreach(var attr in GraphUserConfigService.GetUserAttributeMapping())
                    {
                        _allAttributes.TryAdd(attr.Key, attr.Value);
                    }
                }

                return _allAttributes;
            }
        }

        private Dictionary<string, string> _defaultAttributes = new Dictionary<string, string>()
        {
            { "UPN", "UserPrincipalName" },
            { "Display Name", "DisplayName" },
            { "First Name", "GivenName" },
            { "Last Name", "Surname" },
            { "Identities", "Identities" }
        };

        private Dictionary<string, string> _allAttributes;

        public UserManagementModel(IOptions<GraphOptions> graphOptions, IGraphUserConfigService graphUserConfigService)
        {
            GraphOptions = graphOptions.Value;
            GraphUserConfigService = graphUserConfigService;
        }


        public void OnGet()
        {
            LoadSearchableAttributes();
        }

        private void LoadSearchableAttributes(string selected = null)
        {
            SearchableAttributes = GraphUserConfigService.GetSearchableAttributes().Select(kvp => new SelectListItem
            {
                Text = kvp.Key,
                Value = kvp.Value
            }).ToList();

            SearchableAttributes.Insert(0, new SelectListItem("Login ID", "Identities"));

            if (!string.IsNullOrEmpty(selected))
            {
                SearchableAttributes.First(x => x.Value.Equals(selected)).Selected = true;
            }
        }



        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchValue) || string.IsNullOrWhiteSpace(SelectedAttribute))
                return Page();

            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));

            try
            {
                var attributesToFetch = AllAttributes;

                var users = await graphClient.Users
                    .GetAsync(request =>
                    {
                        request.QueryParameters.Filter = SelectedAttribute.Equals("Identities") ? $"identities/any(id:id/issuer eq 'ExternalMngEnvMCAP508975.onmicrosoft.com' and id/issuerAssignedId eq '{SearchValue}')" : $"{SelectedAttribute} eq '{SearchValue}'";
                        request.QueryParameters.Select = attributesToFetch.Values.ToArray();
                    });

                var user = users?.Value?.FirstOrDefault();

                UserAttributes = user != null
                    ? BuildAttributeDict(user)
                    : new Dictionary<string, string> { { "Error", "User not found." } };
            }
            catch (ServiceException ex)
            {
                UserAttributes = new Dictionary<string, string>
        {
            { "Error", ex.Message }
        };
            }

            LoadSearchableAttributes(SelectedAttribute);
            return Page();
        }


        private Dictionary<string, string> BuildAttributeDict(Microsoft.Graph.Models.User user)
        {
            var result = new Dictionary<string, string>
            {
            };

            foreach (var attributeMapping in AllAttributes)
            {
                if (attributeMapping.Value.StartsWith("extension_"))
                {
                    user.AdditionalData.TryGetValue(attributeMapping.Value, out object val);
                    result.Add(attributeMapping.Key, (string)val);
                }
                else if(attributeMapping.Value.Contains("Identities"))
                {
                    foreach(var identity in user.Identities)
                    {
                        result.Add($"identities.{identity.SignInType}", identity.IssuerAssignedId);
                    }
                }
                else
                {
                    PropertyInfo prop = typeof(Microsoft.Graph.Models.User).GetProperty(attributeMapping.Value);
                    if (prop != null)
                    {
                        result.Add(attributeMapping.Key, (string)prop.GetValue(user));
                        //Console.WriteLine($"Value of {attributeMapping.Value}: {value}");
                    }
                }
            }

            return result;
        }

        private User UpdateUserFromMapping()
        {
            var user = new User() { AdditionalData = new Dictionary<string,object>() };

            foreach (var kv in UserAttributes)
            {
                if (string.IsNullOrEmpty(kv.Value))
                {
                    continue;
                }

                if (AllAttributes.TryGetValue(kv.Key, out string val))
                {
                    if (val.StartsWith("extension"))
                    {
                        user.AdditionalData.TryAdd(val, kv.Value);
                    }
                    else
                    {
                        PropertyInfo prop = typeof(Microsoft.Graph.Models.User).GetProperty(val);

                        if (prop != null && prop.CanWrite)
                        {
                            prop.SetValue(user, Convert.ChangeType(kv.Value, prop.PropertyType));
                        }
                    }
                }
            }

            return user;
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));
            var userUpdate = UpdateUserFromMapping();
            

            try
            {
                await graphClient.Users[userUpdate.UserPrincipalName].PatchAsync(userUpdate);
            }
            catch (ServiceException ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating user: {ex.Message}");
            }

            LoadSearchableAttributes(SelectedAttribute);
            return await OnPostSearchAsync(); // Or return Page() to stay on the same page
        }

    }
}

