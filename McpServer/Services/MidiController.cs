using McpServer.Models;
using NAudio.Midi;
using System.Threading;

namespace McpServer.Services;
internal class MidiController
{
    static bool _isPlaying = false;
    static Dictionary<int, MidiOut> _midiOutDevices = new Dictionary<int, MidiOut>();
    static Sequence? _currentSequence = null;
    static SemaphoreSlim _sequenceLock = new SemaphoreSlim(1, 1);
    static CancellationTokenSource _stopCts = new CancellationTokenSource();
    
    // Queue to hold the next sequence to be played after current completes
    static Sequence? _nextSequence = null;
    
    // Flag to indicate if we have a next sequence queued
    static bool _hasNextSequence = false;

    static MidiController()
    {
        for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            var midiOut = new MidiOut(i);
            _midiOutDevices.Add(i, midiOut);
        }
    }

    internal static List<MidiDevice> ListMidiDevices()
    {
        var devices = new List<MidiDevice>();
        for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            var info = MidiOut.DeviceInfo(i);
            devices.Add(new MidiDevice { Index = i, Name = info.ProductName });
        }
        return devices;
    }

    internal static void StopPlayback()
    {
        _isPlaying = false;
        _stopCts.Cancel();
        
        try
        {
            // Create a new cancellation token for future use
            _stopCts = new CancellationTokenSource();
            
            // Clear any pending sequences
            _nextSequence = null;
            _hasNextSequence = false;
            
            // Make sure to stop all notes
            StopAllNotes();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping playback: {ex.Message}");
        }
    }

    internal static async Task PlaySequence(Sequence sequence)
    {
        // If we're not currently playing, start immediately
        if (!_isPlaying)
        {
            await StartPlayback(sequence);
        }
        else
        {
            // Queue this sequence to play after the current one completes
            _nextSequence = sequence;
            _hasNextSequence = true;
        }
    }

    private static async Task StartPlayback(Sequence sequence)
    {
        // Ensure we have a fresh cancellation token if needed
        if (_stopCts.IsCancellationRequested)
        {
            _stopCts = new CancellationTokenSource();
        }
        var cancellationToken = _stopCts.Token;

        // Acquire the lock for playback
        await _sequenceLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            _isPlaying = true;
            _currentSequence = sequence;

            // Continue playing until explicitly stopped
            while (_isPlaying && !cancellationToken.IsCancellationRequested)
            {
                // Play through all steps in the current sequence
                if (_currentSequence != null && _currentSequence.Steps.Count > 0)
                {
                    foreach (var step in _currentSequence.Steps)
                    {
                        if (!_isPlaying || cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        
                        try
                        {
                            // Play this step
                            await PlayStep(step, cancellationToken);
                        }
                        catch (TaskCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error playing step: {ex.Message}");
                        }
                    }
                }

                // After completing the sequence, check if we should switch to next
                if (_hasNextSequence && _nextSequence != null)
                {
                    _currentSequence = _nextSequence;
                    _nextSequence = null;
                    _hasNextSequence = false;
                }
                // If no next sequence and we're still supposed to be playing, loop
            }
        }
        finally
        {
            // Ensure we release the lock
            if (_sequenceLock.CurrentCount == 0)
            {
                _sequenceLock.Release();
            }
            
            // Stop any notes that may be playing
            StopAllNotes();
        }
    }

    private static async Task PlayStep(Step step, CancellationToken cancellationToken)
    {
        // Start all notes for this step
        var startNoteTasks = step.Triggers.Select(trigger => Task.Run(() =>
        {
            if (!cancellationToken.IsCancellationRequested)
                _midiOutDevices[trigger.DeviceIndex].Send(MidiMessage.StartNote(trigger.Note, trigger.Velocity, trigger.Channel).RawData);
        }));
        await Task.WhenAll(startNoteTasks);

        // Wait for the step duration
        await Task.Delay(step.DurationMs, cancellationToken);

        // Stop all notes for this step
        var stopNoteTasks = step.Triggers.Select(trigger => Task.Run(() =>
        {
            if (!cancellationToken.IsCancellationRequested)
                _midiOutDevices[trigger.DeviceIndex].Send(MidiMessage.StopNote(trigger.Note, 0, trigger.Channel).RawData);
        }));
        await Task.WhenAll(stopNoteTasks);
    }

    private static void StopAllNotes()
    {
        if (_currentSequence != null)
        {
            foreach (var step in _currentSequence.Steps)
            {
                foreach (var trigger in step.Triggers)
                {
                    try
                    {
                        _midiOutDevices[trigger.DeviceIndex].Send(MidiMessage.StopNote(trigger.Note, 0, trigger.Channel).RawData);
                    }
                    catch
                    {
                        // Ignore errors when stopping notes
                    }
                }
            }
        }
    }
}
