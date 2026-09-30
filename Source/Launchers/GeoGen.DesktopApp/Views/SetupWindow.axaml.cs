using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GeoGen.DesktopApp.Services;
using GeoGen.DesktopApp.ViewModels;

namespace GeoGen.DesktopApp.Views;

public partial class SetupWindow : Window
{
    public SetupWindow(StudioSettings settings, bool recommendDrawingTools = false)
    {
        AvaloniaXamlLoader.Load(this);
        var viewModel = new SetupViewModel(this, settings, recommendDrawingTools);
        DataContext = viewModel;
        Opened += async (_, _) =>
        {
            if (recommendDrawingTools)
            {
                this.FindControl<TabControl>("SetupTabs")!.SelectedIndex = 1;
                await viewModel.InstallRecommendedAsync();
            }
            else
                await viewModel.RefreshAsync();
        };
        Closing += (_, args) =>
        {
            // Do not abandon a package manager mid-install. Stop first, then close.
            if (viewModel.IsInstalling)
                args.Cancel = true;
        };
        Closed += (_, _) => viewModel.Dispose();
    }
}
