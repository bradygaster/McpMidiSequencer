using System.Text.Json.Serialization;

namespace McpServer.Models;

public class Sequence
{
    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; } = new();
}
