using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace ControllerExplorer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
        KeyDown += OnKeyDown;
    }

    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            await SaveScreenshotAsync();
            e.Handled = true;
        }
    }

    private async Task SaveScreenshotAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Screenshot",
            SuggestedFileName = $"ControllerExplorer_{DateTime.Now:yyyyMMdd_HHmmss}.png",
            DefaultExtension = "png",
            FileTypeChoices =
            [
                new FilePickerFileType("PNG Image") { Patterns = ["*.png"] }
            ]
        });

        if (file == null) return;

        try
        {
            var pixelSize = new PixelSize((int)Bounds.Width, (int)Bounds.Height);
            var dpi = new Vector(96, 96);

            using var bitmap = new RenderTargetBitmap(pixelSize, dpi);
            bitmap.Render(this);

            await using var stream = await file.OpenWriteAsync();
            bitmap.Save(stream);
        }
        catch (Exception ex)
        {
            // Update status message if ViewModel is available
            if (DataContext is ViewModels.MainWindowViewModel vm)
            {
                vm.StatusMessage = $"Screenshot failed: {ex.Message}";
            }
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
