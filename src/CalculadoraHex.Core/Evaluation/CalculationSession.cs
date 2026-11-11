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

    /// <summary>Disparado após cada linha acrescentada (cálculo, com sucesso ou erro, ou separador).</summary>
    public event EventHandler<CalculationEntry>? EntryAdded;

    /// <summary>Disparado quando a sessão é reiniciada (histórico e variáveis zerados).</summary>
    public event EventHandler? Reset;

    /// <summary>
    /// Disparado quando o histórico foi reconstruído de uma vez (ex.: após apagar a última linha).
    /// Quem exibe o histórico deve redesenhar tudo a partir de <see cref="Entries"/>.
    /// </summary>
    public event EventHandler? HistoryReplaced;

    public CalculationEntry Evaluate(string text, NumberBase inputBase) =>
        Evaluate(text, inputBase, DateTimeOffset.Now);

    public CalculationEntry Evaluate(string text, NumberBase inputBase, DateTimeOffset timestamp)
    {
        var entry = _evaluator.Evaluate(text, inputBase, timestamp);
        Add(entry);
        return entry;
    }

    public CalculationEntry AddSeparator() => AddSeparator(DateTimeOffset.Now);

    public CalculationEntry AddSeparator(DateTimeOffset timestamp)
    {
        var entry = CalculationEntry.CreateSeparator(timestamp);
        Add(entry);
        return entry;
    }

    /// <summary>
    /// Apaga a última linha (cálculo ou separador). As variáveis são recalculadas reavaliando o que sobrou,
    /// de modo que uma atribuição apagada deixa de valer. Retorna falso se não havia linha.
    /// </summary>
    public bool RemoveLast()
    {
        if (_entries.Count == 0) return false;

        _entries.RemoveAt(_entries.Count - 1);
        Rebuild();
        return true;
    }

    public void Clear() => Clear(DateTimeOffset.Now);

    public void Clear(DateTimeOffset createdAt)
    {
        _entries.Clear();
        Variables.Clear();
        CreatedAt = createdAt;
        Reset?.Invoke(this, EventArgs.Empty);
    }

    private void Add(CalculationEntry entry)
    {
        _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
    }

    /// <summary>Reavalia todas as linhas em ordem, do zero, reconstruindo as variáveis.</summary>
    private void Rebuild()
    {
        var previous = _entries.ToList();
        _entries.Clear();
        Variables.Clear();

        foreach (var old in previous)
        {
            _entries.Add(old.IsSeparator
                ? old
                : _evaluator.Evaluate(old.RawText, old.InputBase, old.Timestamp));
        }

        HistoryReplaced?.Invoke(this, EventArgs.Empty);
    }
}
