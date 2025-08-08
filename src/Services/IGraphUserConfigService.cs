namespace src.Services
{
    public interface IGraphUserConfigService
    {
        string GetExtensionAttributeGuid();
        Dictionary<string, string> GetUserAttributeMapping();
    }
}
