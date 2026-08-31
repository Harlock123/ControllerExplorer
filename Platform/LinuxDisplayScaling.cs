using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ControllerExplorer.Platform;

/// <summary>
/// Applies the desktop's display scale on Linux before Avalonia starts.
///
/// Avalonia uses its X11 backend on Linux. Under a Wayland compositor the app
/// therefore runs through XWayland, which is never told about fractional
/// scaling — so on a HiDPI screen the UI renders at 1x and looks tiny while
/// native applications scale correctly. The X11 backend does honour
/// AVALONIA_GLOBAL_SCALE_FACTOR, so the fix is to work out the scale ourselves
/// and set that variable before any Avalonia code runs.
/// </summary>
internal static class LinuxDisplayScaling
{
    private const string GlobalScaleVariable = "AVALONIA_GLOBAL_SCALE_FACTOR";
    private const string PerScreenScaleVariable = "AVALONIA_SCREEN_SCALE_FACTORS";

    // Below 1.0 is never a scale we set; above 4.0 is almost certainly a
    // misparse rather than a real display.
    private const double MinScale = 1.0;
    private const double MaxScale = 4.0;

    public static void Apply()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return;

        // An explicit setting always wins — this is only a fallback.
        if (HasValue(GlobalScaleVariable) || HasValue(PerScreenScaleVariable))
            return;

        // No X11 display means a native backend that handles its own scaling.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
            return;

        var scale = DetectScale();
        if (scale is null || scale <= MinScale || scale > MaxScale)
            return;

        Environment.SetEnvironmentVariable(
            GlobalScaleVariable,
            scale.Value.ToString("0.####", CultureInfo.InvariantCulture));
    }

    private static bool HasValue(string name) =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name));

    private static double? DetectScale() =>
        HyprlandScale() ?? GdkScale() ?? XftDpiScale();

    /// <summary>Exact fractional scale, straight from the compositor.</summary>
    private static double? HyprlandScale()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HYPRLAND_INSTANCE_SIGNATURE")))
            return null;

        var json = RunCommand("hyprctl", "-j monitors");
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            double? first = null;
            foreach (var monitor in document.RootElement.EnumerateArray())
            {
                if (!monitor.TryGetProperty("scale", out var scaleElement) ||
                    !scaleElement.TryGetDouble(out var scale))
                    continue;

                first ??= scale;

                // Prefer the monitor holding focus; it is the one the window opens on.
                if (monitor.TryGetProperty("focused", out var focused) &&
                    focused.ValueKind == JsonValueKind.True)
                    return scale;
            }

            return first;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>GTK's integer scale. Coarse — 1.6 is reported as 2 — but better than 1x.</summary>
    private static double? GdkScale()
    {
        var raw = Environment.GetEnvironmentVariable("GDK_SCALE");
        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
            ? scale
            : null;
    }

    /// <summary>Classic X11 setting, where 96 dpi is 1x.</summary>
    private static double? XftDpiScale()
    {
        var output = RunCommand("xrdb", "-query");
        if (string.IsNullOrWhiteSpace(output))
            return null;

        foreach (var line in output.Split('\n'))
        {
            if (!line.StartsWith("Xft.dpi:", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = line["Xft.dpi:".Length..].Trim();
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var dpi) && dpi > 0)
                return dpi / 96.0;
        }

        return null;
    }

    private static string? RunCommand(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
                return null;

            var output = process.StandardOutput.ReadToEnd();

            // Startup must not hang because a helper misbehaves.
            if (!process.WaitForExit(2000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception)
        {
            // Command missing or not permitted — fall through to the next source.
            return null;
        }
    }
}
