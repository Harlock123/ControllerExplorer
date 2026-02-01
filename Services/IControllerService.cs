using ControllerExplorer.Models;

namespace ControllerExplorer.Services;

public interface IControllerService : IDisposable
{
    IReadOnlyList<ControllerDevice> GetConnectedControllers();

    void StartReading(ControllerDevice device);

    void StopReading();

    bool IsReading { get; }

    event Action<ControllerData>? OnDataReceived;

    event Action<Exception>? OnError;
}
