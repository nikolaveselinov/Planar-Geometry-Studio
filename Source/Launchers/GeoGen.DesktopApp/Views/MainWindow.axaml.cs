using Avalonia.Controls;
using Avalonia.Threading;
using GeoGen.DesktopApp.ViewModels;
using System.ComponentModel;

namespace GeoGen.DesktopApp.Views;

public partial class MainWindow : Window
{
    private TextBox? _outputBox;

    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _outputBox = this.FindControl<TextBox>("OutputBox");

        if (DataContext is MainWindowViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.OutputText))
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_outputBox != null)
                {
                    // Check if we should scroll to top (for help content) or bottom (for output)
                    if (DataContext is MainWindowViewModel vm && vm.ScrollOutputToTop)
                    {
                        _outputBox.CaretIndex = 0;
                        vm.ScrollOutputToTop = false;
                    }
                    else
                    {
                        _outputBox.CaretIndex = _outputBox.Text?.Length ?? 0;
                    }
                }
            }, DispatcherPriority.Background);
        }
    }
}
