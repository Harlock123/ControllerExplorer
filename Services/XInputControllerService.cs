#if WINDOWS
using ControllerExplorer.Models;
using Vortice.XInput;

namespace ControllerExplorer.Services;

public class XInputControllerService : IControllerService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readingTask;
    private bool _disposed;

    public bool IsReading => _readingTask is { IsCompleted: false };

    public event Action<ControllerData>? OnDataReceived;
    public event Action<Exception>? OnError;

    public IReadOnlyList<ControllerDevice> GetConnectedControllers()
    {
        var controllers = new List<ControllerDevice>();

        try
        {
            for (uint i = 0; i < 4; i++)
            {
                if (XInput.GetState(i, out _))
                {
                    var capabilities = XInput.GetCapabilities(i, DeviceQueryType.Any, out var caps);
                    var name = capabilities ? GetControllerName(caps.SubType) : "XInput Controller";
                    controllers.Add(new ControllerDevice((int)i, $"{name} #{i + 1}"));
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
        }

        return controllers;
    }

    private static string GetControllerName(DeviceSubType subType) => subType switch
    {
        DeviceSubType.Gamepad => "Xbox Controller",
        DeviceSubType.Wheel => "Racing Wheel",
        DeviceSubType.ArcadeStick => "Arcade Stick",
        DeviceSubType.FlightStick => "Flight Stick",
        DeviceSubType.DancePad => "Dance Pad",
        DeviceSubType.Guitar => "Guitar Controller",
        DeviceSubType.GuitarAlternate => "Guitar Controller",
        DeviceSubType.GuitarBass => "Bass Guitar",
        DeviceSubType.DrumKit => "Drum Kit",
        DeviceSubType.ArcadePad => "Arcade Pad",
        _ => "XInput Controller"
    };

    public void StartReading(ControllerDevice device)
    {
        if (device.InputType != InputType.XInput)
        {
            throw new ArgumentException("Device is not an XInput device", nameof(device));
        }

        StopReading();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _readingTask = Task.Run(async () =>
        {
            try
            {
                await ReadXInputAsync((uint)device.XInputIndex, token);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }, token);
    }

    private async Task ReadXInputAsync(uint controllerIndex, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (XInput.GetState(controllerIndex, out var state))
            {
                var data = ConvertStateToBytes(state.Gamepad);
                OnDataReceived?.Invoke(new ControllerData(data));
            }
            else
            {
                OnError?.Invoke(new Exception("Controller disconnected"));
                break;
            }

            await Task.Delay(16, ct); // ~60 Hz polling
        }
    }

    private static byte[] ConvertStateToBytes(Gamepad gamepad)
    {
        var bytes = new byte[12];

        // Bytes 0-1: Buttons (GamepadButtons flags as ushort)
        var buttons = (ushort)gamepad.Buttons;
        bytes[0] = (byte)(buttons & 0xFF);
        bytes[1] = (byte)((buttons >> 8) & 0xFF);

        // Byte 2: Left Trigger
        bytes[2] = gamepad.LeftTrigger;

        // Byte 3: Right Trigger
        bytes[3] = gamepad.RightTrigger;

        // Bytes 4-5: Left Thumb X (short as two bytes)
        bytes[4] = (byte)(gamepad.LeftThumbX & 0xFF);
        bytes[5] = (byte)((gamepad.LeftThumbX >> 8) & 0xFF);

        // Bytes 6-7: Left Thumb Y
        bytes[6] = (byte)(gamepad.LeftThumbY & 0xFF);
        bytes[7] = (byte)((gamepad.LeftThumbY >> 8) & 0xFF);

        // Bytes 8-9: Right Thumb X
        bytes[8] = (byte)(gamepad.RightThumbX & 0xFF);
        bytes[9] = (byte)((gamepad.RightThumbX >> 8) & 0xFF);

        // Bytes 10-11: Right Thumb Y
        bytes[10] = (byte)(gamepad.RightThumbY & 0xFF);
        bytes[11] = (byte)((gamepad.RightThumbY >> 8) & 0xFF);

        return bytes;
    }

    public void StopReading()
    {
        _cancellationTokenSource?.Cancel();

        try
        {
            _readingTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch (AggregateException)
        {
            // Task was cancelled, expected
        }

        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _readingTask = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopReading();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
#endif
