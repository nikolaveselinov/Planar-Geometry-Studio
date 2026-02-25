using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GeoGen.DesktopApp.ViewModels;

namespace GeoGen.DesktopApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainWindowViewModel(this);
        DataContext = vm;

        // Auto-scroll the output box when new text is appended
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.OutputText))
            {
                var outputBox = this.FindControl<TextBox>("OutputBox");
                if (outputBox != null)
                {
                    if (vm.ScrollOutputToTop)
                    {
                        outputBox.CaretIndex = 0;
                        vm.ScrollOutputToTop = false;
                    }
                    else
                    {
                        // Move caret to end to trigger auto-scroll
                        outputBox.CaretIndex = outputBox.Text?.Length ?? 0;
                    }
                }
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
