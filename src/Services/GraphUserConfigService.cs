
using src.Options;

namespace src.Services
{
    public class GraphUserConfigService : IGraphUserConfigService
    {
        private readonly Dictionary<string,string> _mapping;
        private readonly Dictionary<string,string> _searchableAttributes;
        private readonly string _extensionAttributeGuid;

        public GraphUserConfigService(IConfiguration configuration)
        {
            _extensionAttributeGuid = configuration.GetSection("ExtensionAttributeGuid").Value;
            _mapping = configuration.GetSection("UserAttributeMappings")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value.StartsWith("extension_") 
                                                && !x.Value.Contains(_extensionAttributeGuid) 
                                                    ? x.Value.Replace("extension_", $"extension_{_extensionAttributeGuid}_") 
                                                    : x.Value);

            _searchableAttributes = configuration.GetSection("SearchableAttributes")
                .GetChildren()
                .ToDictionary(x => x.Key, x => x.Value.StartsWith("extension_")
                                                && !x.Value.Contains(_extensionAttributeGuid)
                                                    ? x.Value.Replace("extension_", $"extension_{_extensionAttributeGuid}_")
                                                    : x.Value);


        }
        public string GetExtensionAttributeGuid()
        {
            return _extensionAttributeGuid;
        }

        public Dictionary<string, string> GetSearchableAttributes()
        {
            return _searchableAttributes;
        }

        public Dictionary<string, string> GetUserAttributeMapping()
        {
            return _mapping;
        }
    }
}
