using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GeoGen.DesktopApp.ViewModels;

namespace GeoGen.DesktopApp.Views;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainWindowViewModel(this);
        DataContext = viewModel;

        viewModel.PropertyChanged += (_, eventArgs) =>
        {
            if (eventArgs.PropertyName != nameof(MainWindowViewModel.OutputText))
                return;

            var outputBox = this.FindControl<TextBox>("OutputBox");
            if (outputBox is null)
                return;

            if (viewModel.ScrollOutputToTop)
            {
                outputBox.CaretIndex = 0;
                viewModel.ScrollOutputToTop = false;
            }
            else
            {
                outputBox.CaretIndex = outputBox.Text?.Length ?? 0;
            }
        };

        Closing += OnClosing;
        Closed += (_, _) => viewModel.Shutdown();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private async void OnClosing(object? sender, WindowClosingEventArgs eventArgs)
    {
        if (_allowClose || DataContext is not MainWindowViewModel viewModel || !viewModel.IsDirty)
            return;

        eventArgs.Cancel = true;
        if (!await viewModel.ConfirmCloseAsync())
            return;

        _allowClose = true;
        Close();
    }
}
