using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GeoGen.DesktopApp.ViewModels;

namespace GeoGen.DesktopApp.Views;

public partial class ConstructionBrowserWindow : Window
{
    public ConstructionBrowserWindow()
    {
        AvaloniaXamlLoader.Load(this);
        DataContext = new ConstructionBrowserViewModel();
        Opened += (_, _) => this.FindControl<TextBox>("SearchBox")?.Focus();
    }

    private void EnableConstruction(object? sender, RoutedEventArgs args)
    {
        if (DataContext is ConstructionBrowserViewModel { Selected: { } entry }) Close(entry.Name);
    }

    private async void CopyCall(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not ConstructionBrowserViewModel { Selected: { } entry } viewModel) return;
        try
        {
            if (Clipboard is null) throw new InvalidOperationException("Clipboard unavailable.");
            await Clipboard.SetValueAsync(DataFormat.Text, entry.Invocation);
            viewModel.Feedback = "Example copied.";
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or
            UnauthorizedAccessException or PlatformNotSupportedException)
        {
            viewModel.Feedback = "Clipboard unavailable. Select the example and copy it manually.";
        }
    }

    private void CloseBrowser(object? sender, RoutedEventArgs args) => Close();
}
