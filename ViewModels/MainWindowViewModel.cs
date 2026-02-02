using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerExplorer.Models;
using ControllerExplorer.Services;

namespace ControllerExplorer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IControllerService _controllerService;
    private readonly DispatcherTimer _changeHighlightTimer;
    private bool _disposed;

    [ObservableProperty]
    private ControllerViewModel? _selectedController;

    [ObservableProperty]
    private bool _isReading;

    [ObservableProperty]
    private string _statusMessage = "Click Refresh to scan for controllers";

    public ObservableCollection<ControllerViewModel> Controllers { get; } = [];

    public MainWindowViewModel() : this(new CompositeControllerService())
    {
    }

    public MainWindowViewModel(IControllerService controllerService)
    {
        _controllerService = controllerService;
        _controllerService.OnDataReceived += HandleDataReceived;
        _controllerService.OnError += HandleError;

        _changeHighlightTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _changeHighlightTimer.Tick += OnChangeHighlightTimerTick;
        _changeHighlightTimer.Start();
    }

    partial void OnSelectedControllerChanged(ControllerViewModel? oldValue, ControllerViewModel? newValue)
    {
        if (oldValue != null)
        {
            oldValue.IsSelected = false;
        }

        _controllerService.StopReading();
        IsReading = false;

        if (newValue != null)
        {
            newValue.IsSelected = true;
            StartReading(newValue);
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        _controllerService.StopReading();
        IsReading = false;
        SelectedController = null;
        Controllers.Clear();

        var devices = _controllerService.GetConnectedControllers();

        foreach (var device in devices)
        {
            Controllers.Add(new ControllerViewModel(device));
        }

        StatusMessage = devices.Count == 0
            ? "No controllers found. Connect a controller and click Refresh."
            : $"Found {devices.Count} controller(s). Select one to monitor.";
    }

    private void StartReading(ControllerViewModel controller)
    {
        try
        {
            _controllerService.StartReading(controller.Device);
            IsReading = true;
            StatusMessage = $"Reading from {controller.ProductName}...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsReading = false;
        }
    }

    private void HandleDataReceived(ControllerData data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            SelectedController?.UpdateData(data);
        });
    }

    private void HandleError(Exception ex)
    {
        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = $"Error: {ex.Message}";
            IsReading = false;
        });
    }

    private void OnChangeHighlightTimerTick(object? sender, EventArgs e)
    {
        SelectedController?.ClearChangeFlags();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _changeHighlightTimer.Stop();
        _controllerService.OnDataReceived -= HandleDataReceived;
        _controllerService.OnError -= HandleError;
        _controllerService.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
