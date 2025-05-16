using System.Text.Json.Serialization;

namespace McpServer.Models;

public class Trigger
{
    [JsonPropertyName("deviceIndex")]
    public int DeviceIndex { get; set; }

    [JsonPropertyName("channel")]
    public int Channel { get; set; }

    [JsonPropertyName("note")]
    public int Note { get; set; }

    [JsonPropertyName("velocity")]
    public int Velocity { get; set; } = 100; // Default velocity for MIDI note on messages
}
