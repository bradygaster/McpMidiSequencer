using System.Text.Json.Serialization;

namespace McpServer.Models;

public class Step
{
    [JsonPropertyName("triggers")]
    public List<Trigger> Triggers { get; set; } = new();

    [JsonPropertyName("durationms")]
    public int DurationMs { get; set; }
}
