using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Custom_Mcp.Tools;

public static class HttpClientExtensions
{
    public static async Task<JsonDocument> ReadJsonDocumentAsync(this HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
} 