using McpServer.Models;
using McpServer.Services;
using McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpMidiTool>();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapGet("/midi/devices", () => MidiController.ListMidiDevices())
   .WithName("ListMidiDevices"); 

app.MapPost("/midi/play", (Sequence sequence) =>
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    MidiController.PlaySequence(sequence); // wait is not used here to allow immediate response
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    return Results.Ok(new { message = "Playback started" });
});

app.MapPost("/midi/stop", () =>
{
    MidiController.StopPlayback();
    return Results.Ok(new { message = "Playback stopped" });
});

app.MapMcp();

app.Run();
