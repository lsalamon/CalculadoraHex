namespace CalculadoraHex.Core.Evaluation;

/// <summary>
/// O histórico único da calculadora: variáveis + lista ordenada de linhas avaliadas.
/// Os painéis (hexa, decimal) são apenas visões deste objeto.
/// </summary>
public sealed class CalculationSession
{
    private readonly List<CalculationEntry> _entries = new();
    private readonly Evaluator _evaluator;

    public CalculationSession()
    {
        Variables = new VariableStore();
        _evaluator = new Evaluator(Variables);
    }

    public VariableStore Variables { get; }

    public IReadOnlyList<CalculationEntry> Entries => _entries;

    /// <summary>Disparado após cada linha avaliada, com sucesso ou erro.</summary>
    public event EventHandler<CalculationEntry>? EntryAdded;

    /// <summary>Disparado quando a sessão é reiniciada (histórico e variáveis zerados).</summary>
    public event EventHandler? Reset;

    public CalculationEntry Evaluate(string text, NumberBase inputBase)
    {
        var entry = _evaluator.Evaluate(text, inputBase);
        _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
        return entry;
    }

    public void Clear()
    {
        _entries.Clear();
        Variables.Clear();
        Reset?.Invoke(this, EventArgs.Empty);
    }
}
