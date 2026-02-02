using ControllerExplorer.Models;
using HidSharp;

namespace ControllerExplorer.Services;

public class HidControllerService : IControllerService
{
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readingTask;
    private bool _disposed;

    public bool IsReading => _readingTask is { IsCompleted: false };

    public event Action<ControllerData>? OnDataReceived;
    public event Action<Exception>? OnError;

    public IReadOnlyList<ControllerDevice> GetConnectedControllers()
    {
        try
        {
            var devices = DeviceList.Local.GetHidDevices()
                .Where(IsGameController)
                .Select(d => new ControllerDevice(d))
                .ToList();

            return devices;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(ex);
            return [];
        }
    }

    private static bool IsGameController(HidDevice device)
    {
        try
        {
            // Filter for devices that have input reports (controllers typically do)
            if (device.GetMaxInputReportLength() <= 0)
                return false;

            // Get usage page and usage to filter for game controllers
            // Usage Page 0x01 = Generic Desktop Controls
            // Usage 0x04 = Joystick, 0x05 = Game Pad
            var reportDescriptor = device.GetReportDescriptor();
            foreach (var deviceItem in reportDescriptor.DeviceItems)
            {
                foreach (var usage in deviceItem.Usages.GetAllValues())
                {
                    var usagePage = (usage >> 16) & 0xFFFF;
                    var usageId = usage & 0xFFFF;

                    // Generic Desktop Controls page (0x01) with Joystick (0x04) or Game Pad (0x05)
                    if (usagePage == 0x01 && (usageId == 0x04 || usageId == 0x05))
                        return true;
                }
            }

            return false;
        }
        catch
        {
            // If we can't read the descriptor, include it anyway if it looks like a controller
            // based on having a reasonable input report length
            return device.GetMaxInputReportLength() > 1 && device.GetMaxInputReportLength() <= 64;
        }
    }

    public void StartReading(ControllerDevice device)
    {
        StopReading();

        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        _readingTask = Task.Run(async () =>
        {
            try
            {
                await ReadControllerAsync(device, token);
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

    private async Task ReadControllerAsync(ControllerDevice device, CancellationToken ct)
    {
        if (device.HidDevice is null)
            throw new InvalidOperationException("Cannot read from a device without HID support");

        using var stream = device.HidDevice.Open();
        stream.ReadTimeout = 100; // Short timeout for responsive cancellation

        var buffer = new byte[device.MaxInputReportLength];

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var bytesRead = await Task.Run(() =>
                {
                    try
                    {
                        return stream.Read(buffer, 0, buffer.Length);
                    }
                    catch (TimeoutException)
                    {
                        return 0;
                    }
                }, ct);

                if (bytesRead > 0)
                {
                    var data = new ControllerData(buffer);
                    OnDataReceived?.Invoke(data);
                }
            }
            catch (TimeoutException)
            {
                // No data available, continue
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
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
