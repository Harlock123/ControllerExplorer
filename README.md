# Controller Explorer

A cross-platform .NET 9 application built with Avalonia UI for reading and displaying real-time data from USB and Bluetooth game controllers. This tool assists in creating games that read from these controllers by providing visibility into the raw HID data.

## Features

- **Multi-controller support** - Detects and lists all connected USB/Bluetooth game controllers
- **Real-time data display** - Shows 9 bytes of controller data updating continuously
- **Hex + Binary visualization** - Each byte displayed as hex (e.g., `0x3F`) with full 8-bit binary breakdown
- **Change highlighting** - Visual indicator when byte values change
- **Cross-platform** - Runs on Windows, macOS, and Linux
- **Screenshot export** - Save the current view as a PNG image

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save screenshot as PNG |

## Screenshots

The application displays:
- Left panel: List of detected controllers with VID:PID identifiers
- Right panel: Controller metadata and 9-byte data grid with hex/binary values
- Status bar: Connection status and last update timestamp

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A USB or Bluetooth game controller

## Building

```bash
# Clone the repository
git clone https://github.com/yourusername/ControllerExplorer.git
cd ControllerExplorer

# Build
dotnet build

# Run
dotnet run
```

## Usage

1. Connect a USB or Bluetooth game controller to your computer
2. Launch the application
3. Click **Refresh Controllers** to scan for connected devices
4. Select a controller from the list
5. Interact with the controller (press buttons, move sticks, triggers)
6. Observe the byte values updating in real-time

## Platform Notes

### macOS
You may need to grant accessibility permissions for the application to read HID devices. Go to **System Preferences > Security & Privacy > Privacy > Input Monitoring** and add the application.

### Linux
You may need to configure udev rules to allow non-root access to HID devices:

```bash
# Create a udev rule (example)
sudo nano /etc/udev/rules.d/99-hid.rules

# Add a rule for your controller (replace VID and PID)
SUBSYSTEM=="hidraw", ATTRS{idVendor}=="xxxx", ATTRS{idProduct}=="yyyy", MODE="0666"

# Reload rules
sudo udevadm control --reload-rules
```

## Technology Stack

- **.NET 9** - Target framework
- **Avalonia UI 11.x** - Cross-platform UI framework
- **HidSharp** - Cross-platform HID device library
- **CommunityToolkit.Mvvm** - MVVM framework

## Project Structure

```
ControllerExplorer/
├── Models/
│   ├── ControllerDevice.cs    # HID device wrapper
│   └── ControllerData.cs      # 9-byte data container
├── Services/
│   ├── IControllerService.cs  # Service interface
│   └── HidControllerService.cs # HID reading implementation
├── ViewModels/
│   ├── ByteDisplayViewModel.cs # Individual byte display
│   ├── ControllerViewModel.cs  # Controller with bytes
│   └── MainWindowViewModel.cs  # Main orchestration
├── Views/
│   └── MainWindow.axaml       # Main UI
└── Converters/
    └── BoolToColorConverter.cs # Status indicator
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Author

Lonnie Watson
