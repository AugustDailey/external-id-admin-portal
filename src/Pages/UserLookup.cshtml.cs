using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Azure.Identity;
using Microsoft.Extensions.Options;
using src.Options;
using Microsoft.Graph.Models;
using src.Services;
using System.Reflection;

namespace src.Pages
{
    public class UserLookupModel : PageModel
    {
        [BindProperty]
        public string UPN { get; set; }

        [BindProperty]
        public Dictionary<string, string> UserAttributes { get; set; }
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
            { "Display Name", "DisplayName" },
            { "First Name", "GivenName" },
            { "Last Name", "Surname" },
            { "Identities", "Identities" }
        };

        private Dictionary<string, string> _allAttributes;

        public UserLookupModel(IOptions<GraphOptions> graphOptions, IGraphUserConfigService graphUserConfigService)
        {
            GraphOptions = graphOptions.Value;
            GraphUserConfigService = graphUserConfigService;
        }


        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(UPN))
                return Page();

            var graphClient = new GraphServiceClient(new ClientSecretCredential(GraphOptions.TenantId, GraphOptions.ClientId, GraphOptions.ClientSecret));

            try
            {
                var attributesToFetch = AllAttributes;
                var user = await graphClient.Users[UPN].GetAsync((requestConfiguration) =>
                {
                    requestConfiguration.QueryParameters.Select = attributesToFetch.Values.ToArray();
                });

                UserAttributes = BuildAttributeDict(user);
                
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

        private Dictionary<string, string> BuildAttributeDict(Microsoft.Graph.Models.User user)
        {
            var result = new Dictionary<string, string>
            {
            };

            foreach (var attributeMapping in AllAttributes)
            {
                if (attributeMapping.Value.StartsWith("extension_"))
                {
                    result.Add(attributeMapping.Key, (string)user.AdditionalData[attributeMapping.Value]);
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
            var user = await graphClient.Users[UPN].GetAsync((requestConfiguration) =>
            {
                requestConfiguration.QueryParameters.Select = AllAttributes.Values.ToArray();
            });

            var userUpdate = UpdateUserFromMapping();
            

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

