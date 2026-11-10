using System.Windows;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalculadoraHex.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    public MainViewModel()
    {
        Session = new CalculationSession();
        HexPane = new PaneViewModel(Session, NumberBase.Hexadecimal);
        DecimalPane = new PaneViewModel(Session, NumberBase.Decimal);

        Session.EntryAdded += (_, entry) =>
        {
            HexPane.Append(entry);
            DecimalPane.Append(entry);
        };
        Session.Reset += (_, _) =>
        {
            HexPane.ClearLines();
            DecimalPane.ClearLines();
        };
        Session.Variables.Changed += (_, _) => UpdateVariablesSummary();

        UpdateVariablesSummary();
    }

    public CalculationSession Session { get; }

    public PaneViewModel HexPane { get; }

    public PaneViewModel DecimalPane { get; }

    [ObservableProperty]
    private string _variablesSummary = string.Empty;

    [ObservableProperty]
    private string _sessionName = "Sessão não gravada";

    [RelayCommand]
    private void NewSession()
    {
        if (Session.Entries.Count > 0)
        {
            var answer = MessageBox.Show(
                "Iniciar uma nova sessão apaga o histórico e as variáveis atuais. Continuar?",
                "Nova sessão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;
        }

        Session.Clear();
    }

    /// <summary>Limpa apenas o que está visível; o histórico da sessão permanece.</summary>
    [RelayCommand]
    private void ClearDisplay()
    {
        HexPane.ClearLines();
        DecimalPane.ClearLines();
    }

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    /// <summary>A janela principal assina este evento e abre o diálogo de variáveis (a VM não conhece janelas).</summary>
    public event EventHandler? ShowVariablesRequested;

    [RelayCommand]
    private void ShowVariables() => ShowVariablesRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ClearVariables()
    {
        if (Session.Variables.Count == 0) return;

        var answer = MessageBox.Show(
            "Remover todas as variáveis? O histórico do display é mantido.",
            "Limpar variáveis", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes)
            Session.Variables.Clear();
    }

    [RelayCommand]
    private void ShowAbout() => MessageBox.Show(
        "CalculadoraHex\n\nCalculadora hexadecimal/decimal com painéis sincronizados, variáveis e histórico automático.\n\n" +
        "Sintaxe: [nome =] expressão, com + - * / e parênteses.\n" +
        "No painel hexa, 'a'..'f' são variáveis se definidas, senão dígitos. Use 0x para forçar número e $ para forçar variável.",
        "Sobre", MessageBoxButton.OK, MessageBoxImage.Information);

    private void UpdateVariablesSummary()
    {
        var vars = Session.Variables.Snapshot();
        if (vars.Count == 0)
        {
            VariablesSummary = "Nenhuma variável definida";
            return;
        }

        VariablesSummary = string.Join("    ", vars
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key} = {NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal)}h ({NumberFormatter.Format(kv.Value, NumberBase.Decimal)})"));
    }
}
