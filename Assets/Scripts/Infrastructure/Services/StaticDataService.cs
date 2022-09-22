using Configs;

namespace Infrastructure.Services
{
    public class StaticDataService
    {
        public void Load()
        {
            // load configs by AssetReference
        }

        public ConfigBase TryGetConfig()
        {
            // return cached config instance by config id
            return null;
        }
    }
}
