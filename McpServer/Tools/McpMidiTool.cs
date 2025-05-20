using McpServer.Models;
using McpServer.Services;
using ModelContextProtocol.Server;
using NJsonSchema;
using System.ComponentModel;
using System.Text.Json;

namespace McpServer.Tools;

[McpServerToolType]
public class McpMidiTool
{

    [McpServerTool]
    [Description("Gets the list of available MIDI devices.")]
    public List<MidiDevice> GetMidiDevices()
    {
        return MidiController.ListMidiDevices();
    }

    [McpServerTool]
    [Description("Returns the JSON Schema for the Sequence object, to help users understand how to structure their song data.")]
    public string GetSequenceSchema()
    {
        // Generate JSON schema for the Sequence class
        var schema = JsonSchema.FromType<Sequence>();

        // Add title and description to make it more user-friendly
        schema.Title = "MIDI Sequence Schema";
        schema.Description = "Schema for creating MIDI sequences to be played by the MIDI controller.";

        // Provide a simple example in the description to help users get started
        var exampleJson = @"{
          ""steps"": [
            {
              ""triggers"": [
                {
                  ""deviceIndex"": 0,
                  ""channel"": 0,
                  ""note"": 60,
                  ""velocity"": 100
                }
              ],
              ""durationms"": 500
            }
          ]
        }";

        // Add the example to the schema description
        schema.Description += $"\n\nExample sequence:\n{exampleJson}";

        // Convert the schema to a JSON string with indentation
        return schema.ToJson();
    }

    [McpServerTool]
    [Description("Plays a sequence based on a JSON format. Using this tool results in the entire sequence being played repeatedly or looped until the Stop tool is called.")]
    public void Start(string sequenceJson)
    {   
        var sequence = JsonSerializer.Deserialize<Sequence>(sequenceJson);
        if (sequence == null)
        {
            throw new ArgumentException("Invalid sequence JSON");
        }

        #pragma warning disable CS4014
        // Start playing the sequence asynchronously
        MidiController.PlaySequence(sequence);
        #pragma warning restore CS4014 
    }

    [McpServerTool]
    [Description("Stops playback of the currently-playing sequence.")]
    public void Stop() => MidiController.StopPlayback();
}
