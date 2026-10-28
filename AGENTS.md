# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers features, the byte layouts, platform
setup (udev rules, macOS permissions, display scaling) and downloads; this file covers how the code fits together
and the rules that keep it working.

## What this is

Controller Explorer, a small Avalonia 11 desktop app (C#, .NET 9, CommunityToolkit.Mvvm) that lists connected game
controllers and shows their raw input report bytes live, highlighting the ones that change. It reads HID devices
on every platform through HidSharp, and on Windows also reads Xbox-style controllers through XInput
(Vortice.XInput).

## Commands

```bash
dotnet build ControllerExplorer.sln            # single project, net9.0; there is no global.json
dotnet run                                     # run it from the repo root
dotnet publish ControllerExplorer.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

- There are no tests yet.
- The program takes no command-line arguments. Ctrl+S in the window saves a screenshot as PNG.
- On Linux, `AVALONIA_GLOBAL_SCALE_FACTOR` or `AVALONIA_SCREEN_SCALE_FACTORS` override the detected display scale.

## Layout

The project sits at the repository root.

- `Program.cs`: applies `Platform.LinuxDisplayScaling` before Avalonia starts, then runs the app.
- `Services/`: `IControllerService` (list devices, start and stop reading, `OnDataReceived`, `OnError`).
  - `HidControllerService.cs`: HidSharp; reads reports on a background task.
  - `XInputControllerService.cs`: Windows only, inside `#if WINDOWS`; polls XInput on a background task.
  - `CompositeControllerService.cs`: merges the two and routes reading to the right one.
- `Models/`: `ControllerDevice` (wraps a HID or XInput device) and `ControllerData` (one report's bytes).
- `ViewModels/`: `MainWindowViewModel` (device list, selection, a 200 ms timer that fades change highlights),
  `ControllerViewModel`, `ByteDisplayViewModel`.
- `Views/MainWindow.axaml`: the single window. `Converters/BoolToColorConverter.cs`: the status light.
- `Platform/LinuxDisplayScaling.cs`: works out the desktop scale (Hyprland, `GDK_SCALE`, `Xft.dpi`) for XWayland.
- `Assets/`: the README screenshots and the icon.

## Rules that keep it working

- **XInput stays Windows-only.** The csproj sets `TargetsWindows` from the runtime identifier (or, without one,
  from the building machine), defines `WINDOWS`, and only then references `Vortice.XInput`. Keep all XInput code
  inside `#if WINDOWS` so Linux and macOS builds stay clean, and so a `-r win-*` publish from Linux keeps XInput.
- **Device data arrives off the UI thread.** The services raise their events from background tasks;
  `MainWindowViewModel` posts them to `Dispatcher.UIThread`. Do the same for any new event.
- **Scaling runs first.** `LinuxDisplayScaling.Apply()` must run before any Avalonia code, and an explicit
  scale variable always wins over detection.
- **Reading must stop cleanly.** Services implement `IDisposable`; stop reading before switching devices or
  closing.

## Making a change

- Build with no new warnings and try the change with a real controller, since there are no automated tests.
- If the window changes, update the screenshots in `Assets/` and the README.
- Update the README's byte-layout tables if the decoding changes.

## CI and releases

`.github/workflows/release.yml` runs only on a `v*` tag or by hand with a version number. It publishes
self-contained single-file builds for win-x64/x86/arm64, osx-x64/arm64 and linux-x64/arm64, writes `SHA256SUMS`,
and creates (or adds to) the GitHub release `v<version>`. Ordinary pushes run no CI.
