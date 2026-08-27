using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace GeoGen.DesktopApp.Views;

public enum UnsavedChangesChoice
{
    Cancel,
    Discard,
    Save
}

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    public UnsavedChangesDialog(string fileName)
        : this()
    {
        var message = this.FindControl<TextBlock>("MessageText");
        if (message is not null)
            message.Text = $"{fileName} has changes that have not been saved.";
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void CancelClicked(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Cancel);

    private void DiscardClicked(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Discard);

    private void SaveClicked(object? sender, RoutedEventArgs e) =>
        Close(UnsavedChangesChoice.Save);
}
