namespace Custom_Mcp.Tools.Models
{
    public class FireStoreSettingModel
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ServiceAccountPath { get; set; } = string.Empty;
        public string DefaultCollection { get; set; } = string.Empty;
        public int MaxDocuments { get; set; } = 50;
        public bool DebugMode { get; set; } = false;
        public string LogLevel { get; set; } = "Info";
    }
}
