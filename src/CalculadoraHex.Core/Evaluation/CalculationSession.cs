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
        CreatedAt = DateTimeOffset.Now;
    }

    public VariableStore Variables { get; }

    public IReadOnlyList<CalculationEntry> Entries => _entries;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Disparado após cada linha avaliada, com sucesso ou erro.</summary>
    public event EventHandler<CalculationEntry>? EntryAdded;

    /// <summary>Disparado quando a sessão é reiniciada (histórico e variáveis zerados).</summary>
    public event EventHandler? Reset;

    public CalculationEntry Evaluate(string text, NumberBase inputBase) =>
        Evaluate(text, inputBase, DateTimeOffset.Now);

    public CalculationEntry Evaluate(string text, NumberBase inputBase, DateTimeOffset timestamp)
    {
        var entry = _evaluator.Evaluate(text, inputBase, timestamp);
        _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
        return entry;
    }

    public void Clear() => Clear(DateTimeOffset.Now);

    public void Clear(DateTimeOffset createdAt)
    {
        _entries.Clear();
        Variables.Clear();
        CreatedAt = createdAt;
        Reset?.Invoke(this, EventArgs.Empty);
    }
}
