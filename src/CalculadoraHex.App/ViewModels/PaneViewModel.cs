using System.Collections.ObjectModel;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CalculadoraHex.App.ViewModels;

/// <summary>Um painel (hexa ou decimal): visão do histórico compartilhado na sua base, mais a linha de entrada.</summary>
public sealed partial class PaneViewModel : ObservableObject
{
    private readonly CalculationSession _session;

    public PaneViewModel(CalculationSession session, NumberBase numberBase)
    {
        _session = session;
        Base = numberBase;
    }

    public NumberBase Base { get; }

    public string Title => Base == NumberBase.Hexadecimal ? "HEXADECIMAL" : "DECIMAL";

    public string Hint => Base == NumberBase.Hexadecimal
        ? "Dígitos 0-9 A-F. Ex.: a=1234   a=a*4321   0xFF+1   #comentário"
        : "Dígitos 0-9. Ex.: a=4660   a=a*17185   (2+3)*4   #comentário";

    public ObservableCollection<DisplayLine> Lines { get; } = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>
    /// Verdadeiro enquanto uma função está "digitando" na linha de entrada: o usuário não pode editar nem enviar
    /// o que está sendo escrito, senão suas teclas se misturariam com as da função.
    /// </summary>
    [ObservableProperty]
    private bool _isInputLocked;

    [RelayCommand]
    private void Submit()
    {
        if (IsInputLocked) return;

        var text = InputText.Trim();
        if (text.Length == 0) return;

        _session.Evaluate(text, Base);
        InputText = string.Empty;
    }

    [RelayCommand]
    private void ClearInput()
    {
        if (!IsInputLocked) InputText = string.Empty;
    }

    internal void Append(CalculationEntry entry) => Lines.Add(DisplayLine.From(entry, Base));

    internal void ClearLines() => Lines.Clear();

    /// <summary>Redesenha o display inteiro a partir do histórico (após apagar a última linha, por exemplo).</summary>
    internal void Rebuild(IEnumerable<CalculationEntry> entries)
    {
        Lines.Clear();
        foreach (var entry in entries)
            Lines.Add(DisplayLine.From(entry, Base));
    }
}
