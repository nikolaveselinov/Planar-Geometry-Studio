using GeoGen.DesktopApp.Helpers;
using GeoGen.DesktopApp.Services;

namespace GeoGen.DesktopApp.ViewModels;

public sealed class ConstructionBrowserViewModel : ViewModelBase
{
    private string _feedback = string.Empty;
    private string _query = string.Empty;
    private string _outputType = "All";
    private ConstructionEntry? _selected;
    private IReadOnlyList<ConstructionEntry> _results = ConstructionCatalog.Entries;

    public ConstructionBrowserViewModel() => Selected = _results.FirstOrDefault();

    public static IReadOnlyList<string> OutputTypes { get; } = new[] { "All", "Point", "Line", "Circle" };
    public IReadOnlyList<ConstructionEntry> Results => _results;
    public string ResultCount => $"{Results.Count} of {ConstructionCatalog.Entries.Count} constructions";
    public bool HasSelection => Selected is not null;
    public string SelectedName => Selected?.Name ?? "No matches";
    public string SelectedDescription => Selected?.Description ?? "Try a shorter search or a different object type.";
    public string SelectedInvocation => Selected?.Invocation ?? string.Empty;
    public string SelectedArguments => Selected is null ? string.Empty : $"Arguments: {Selected.ArgumentTypes}";
    public string SelectedOutput => Selected is null ? string.Empty : $"Creates a {Selected.OutputType.ToLowerInvariant()}";

    public string Feedback { get => _feedback; set => SetProperty(ref _feedback, value); }

    public string Query
    {
        get => _query;
        set { if (SetProperty(ref _query, value)) Filter(); }
    }

    public string OutputType
    {
        get => _outputType;
        set { if (SetProperty(ref _outputType, value)) Filter(); }
    }

    public ConstructionEntry? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            Feedback = string.Empty;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedName));
            OnPropertyChanged(nameof(SelectedDescription));
            OnPropertyChanged(nameof(SelectedInvocation));
            OnPropertyChanged(nameof(SelectedArguments));
            OnPropertyChanged(nameof(SelectedOutput));
        }
    }

    private void Filter()
    {
        _results = ConstructionCatalog.Search(Query, OutputType);
        OnPropertyChanged(nameof(Results));
        OnPropertyChanged(nameof(ResultCount));
        if (Selected is null || !_results.Contains(Selected)) Selected = _results.FirstOrDefault();
    }
}
