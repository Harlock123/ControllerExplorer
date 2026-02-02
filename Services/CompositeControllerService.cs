using ControllerExplorer.Models;

namespace ControllerExplorer.Services;

public class CompositeControllerService : IControllerService
{
    private readonly HidControllerService _hidService;
#if WINDOWS
    private readonly XInputControllerService _xInputService;
#endif
    private IControllerService? _activeService;
    private bool _disposed;

    public bool IsReading => _activeService?.IsReading ?? false;

    public event Action<ControllerData>? OnDataReceived;
    public event Action<Exception>? OnError;

    public CompositeControllerService()
    {
        _hidService = new HidControllerService();
        _hidService.OnDataReceived += data => OnDataReceived?.Invoke(data);
        _hidService.OnError += ex => OnError?.Invoke(ex);

#if WINDOWS
        _xInputService = new XInputControllerService();
        _xInputService.OnDataReceived += data => OnDataReceived?.Invoke(data);
        _xInputService.OnError += ex => OnError?.Invoke(ex);
#endif
    }

    public IReadOnlyList<ControllerDevice> GetConnectedControllers()
    {
        var controllers = new List<ControllerDevice>();

#if WINDOWS
        // Add XInput controllers first (they take priority on Windows)
        var xInputControllers = _xInputService.GetConnectedControllers();
        controllers.AddRange(xInputControllers);
#endif

        // Add HID controllers
        var hidControllers = _hidService.GetConnectedControllers();
        controllers.AddRange(hidControllers);

        return controllers;
    }

    public void StartReading(ControllerDevice device)
    {
        StopReading();

#if WINDOWS
        if (device.InputType == InputType.XInput)
        {
            _activeService = _xInputService;
        }
        else
        {
            _activeService = _hidService;
        }
#else
        _activeService = _hidService;
#endif

        _activeService.StartReading(device);
    }

    public void StopReading()
    {
        _activeService?.StopReading();
        _activeService = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopReading();
        _hidService.Dispose();
#if WINDOWS
        _xInputService.Dispose();
#endif
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
