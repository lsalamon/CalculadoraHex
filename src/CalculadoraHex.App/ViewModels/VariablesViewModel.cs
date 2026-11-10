using System.Collections.ObjectModel;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalculadoraHex.App.ViewModels;

public sealed record VariableRow(string Name, string Hex, string Decimal);

/// <summary>Lista as variáveis nas duas bases e permite remover uma ou todas.</summary>
public sealed partial class VariablesViewModel : ObservableObject
{
    private readonly VariableStore _store;

    public VariablesViewModel(VariableStore store)
    {
        _store = store;
        _store.Changed += (_, _) => Refresh();
        Refresh();
    }

    public ObservableCollection<VariableRow> Rows { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    private VariableRow? _selected;

    public bool HasRows => Rows.Count > 0;

    [RelayCommand(CanExecute = nameof(CanRemoveSelected))]
    private void RemoveSelected()
    {
        if (Selected is null) return;
        _store.Remove(Selected.Name);
    }

    private bool CanRemoveSelected() => Selected is not null;

    [RelayCommand]
    private void RemoveAll() => _store.Clear();

    private void Refresh()
    {
        var selectedName = Selected?.Name;
        Rows.Clear();
        foreach (var kv in _store.Snapshot().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            Rows.Add(new VariableRow(
                kv.Key,
                NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal),
                NumberFormatter.Format(kv.Value, NumberBase.Decimal)));
        }
        Selected = Rows.FirstOrDefault(r => string.Equals(r.Name, selectedName, StringComparison.OrdinalIgnoreCase));
        OnPropertyChanged(nameof(HasRows));
    }
}
