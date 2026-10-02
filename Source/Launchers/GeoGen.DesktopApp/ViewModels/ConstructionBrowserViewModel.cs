using GeoGen.DesktopApp.Helpers;
using GeoGen.DesktopApp.Services;

namespace GeoGen.DesktopApp.ViewModels;

public sealed class ConstructionOptionViewModel : ViewModelBase
{
    private readonly Action<ConstructionOptionViewModel, bool> _change;
    private bool _isIncluded;

    public ConstructionOptionViewModel(ConstructionEntry entry, bool included, bool canEdit,
        Action<ConstructionOptionViewModel, bool> change)
    {
        Entry = entry;
        _isIncluded = included;
        CanEdit = canEdit;
        _change = change;
    }

    public ConstructionEntry Entry { get; }
    public string Name => Entry.Name;
    public string OutputType => Entry.OutputType;
    public bool CanEdit { get; }
    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (value == _isIncluded || !CanEdit) return;
            _change(this, value);
            SetProperty(ref _isIncluded, value);
        }
    }
}

public sealed class ConstructionBrowserViewModel : ViewModelBase
{
    private readonly ConstructionOptionViewModel[] _options;
    private string _feedback = string.Empty;
    private string _query = string.Empty;
    private string _outputType = "All";
    private ConstructionOptionViewModel? _selected;
    private IReadOnlyList<ConstructionOptionViewModel> _results;

    public ConstructionBrowserViewModel() : this(StarterConfiguration.Text) { }

    public ConstructionBrowserViewModel(string input)
    {
        InputText = input;
        IReadOnlySet<string> enabled;
        try
        {
            enabled = ConstructionCatalog.GetEnabled(input);
            CanEdit = true;
        }
        catch (ArgumentException)
        {
            enabled = new HashSet<string>();
            Feedback = "Add Constructions: before Initial configuration: in your input to use the checkboxes.";
        }
        _options = ConstructionCatalog.Entries.Select(entry => new ConstructionOptionViewModel(
            entry, enabled.Contains(entry.Name), CanEdit, ChangeConstruction)).ToArray();
        foreach (var option in _options)
            option.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ConstructionOptionViewModel.IsIncluded))
                    OnPropertyChanged(nameof(EnabledCount));
            };
        _results = _options;
        Selected = _results.FirstOrDefault();
    }

    public event Action<string>? ConfigurationChanged;
    public string InputText { get; private set; }
    public bool CanEdit { get; }
    public static IReadOnlyList<string> OutputTypes { get; } = new[] { "All", "Point", "Line", "Circle" };
    public IReadOnlyList<ConstructionOptionViewModel> Results => _results;
    public string ResultCount => $"{Results.Count} of {_options.Length} constructions";
    public string EnabledCount => $"{_options.Count(option => option.IsIncluded)} enabled for generation";
    public bool HasSelection => Selected is not null;
    public string SelectedName => Selected?.Name ?? "No matches";
    public string SelectedDescription => Selected?.Entry.Description ?? "Try a shorter search or a different object type.";
    public string SelectedInvocation => Selected?.Entry.Invocation ?? string.Empty;
    public string SelectedArguments => Selected is null ? string.Empty : $"Arguments: {Selected.Entry.ArgumentTypes}";
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

    public ConstructionOptionViewModel? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value)) return;
            if (CanEdit) Feedback = string.Empty;
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(SelectedName));
            OnPropertyChanged(nameof(SelectedDescription));
            OnPropertyChanged(nameof(SelectedInvocation));
            OnPropertyChanged(nameof(SelectedArguments));
            OnPropertyChanged(nameof(SelectedOutput));
        }
    }

    private void ChangeConstruction(ConstructionOptionViewModel option, bool included)
    {
        Selected = option;
        InputText = ConstructionCatalog.SetEnabled(InputText, option.Name, included);
        Feedback = included ? $"Enabled {option.Name}." : $"Disabled {option.Name}.";
        ConfigurationChanged?.Invoke(InputText);
    }

    private void Filter()
    {
        var matches = ConstructionCatalog.Search(Query, OutputType).ToHashSet();
        _results = _options.Where(option => matches.Contains(option.Entry)).ToArray();
        OnPropertyChanged(nameof(Results));
        OnPropertyChanged(nameof(ResultCount));
        if (Selected is null || !_results.Contains(Selected)) Selected = _results.FirstOrDefault();
    }
}
