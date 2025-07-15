using Custom_Mcp.Tools.Models;
using Microsoft.Extensions.Configuration;

namespace Custom_Mcp.Helpers
{
    public static class FirestoreConfigHelper
    {
        public static FireStoreSettingModel GetSettings()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            return config.GetSection("Firestore").Get<FireStoreSettingModel>() ?? new FireStoreSettingModel();
        }
    }
}
