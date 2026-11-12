using System.Numerics;
using CalculadoraHex.Core.Functions;

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

    /// <summary>
    /// Avalia uma linha digitada. Texto que começa por '#' não é avaliado: vira um comentário literal.
    /// </summary>
    public CalculationEntry Evaluate(string text, NumberBase inputBase, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(text);

        var trimmed = text.Trim();
        if (trimmed.StartsWith('#'))
            return AddComment(trimmed, inputBase, timestamp);

        var entry = _evaluator.Evaluate(text, inputBase, timestamp);
        Add(entry);
        return entry;
    }

    public CalculationEntry AddComment(string text, NumberBase inputBase = NumberBase.Decimal, DateTimeOffset? timestamp = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var entry = CalculationEntry.CreateComment(text.Trim(), inputBase, timestamp ?? DateTimeOffset.Now);
        Add(entry);
        return entry;
    }

    /// <summary>
    /// Calcula a raiz inteira de n fatores do valor que a variável guarda agora e acrescenta o resultado ao histórico.
    /// Lança <see cref="CalculationException"/> se a variável não existe ou se o valor ou n são inválidos.
    /// </summary>
    public CalculationEntry AddRootResult(string variableName, int n)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);
        if (!Variables.TryGet(variableName, out var value))
            throw new CalculationException($"Variável '{variableName}' não definida.");

        return AddRootResult(variableName, value, n, DateTimeOffset.Now);
    }

    public CalculationEntry AddRootResult(string variableName, BigInteger value, int n, DateTimeOffset timestamp)
    {
        var result = IntegerRoot.Compute(value, n);
        var entry = CalculationEntry.CreateRootResult(variableName, result, timestamp);
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

    public CalculationEntry AddVariablePrint(string name, System.Numerics.BigInteger value) =>
        AddVariablePrint(name, value, DateTimeOffset.Now);

    public CalculationEntry AddVariablePrint(string name, System.Numerics.BigInteger value, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var entry = CalculationEntry.CreateVariablePrint(name, value, timestamp);
        Add(entry);
        return entry;
    }

    /// <summary>
    /// Imprime todas as variáveis no histórico, em ordem alfabética, entre linhas separadoras.
    /// O separador anterior não é repetido se a última linha já for um. Retorna quantas variáveis foram impressas.
    /// </summary>
    public int PrintVariables()
    {
        var snapshot = Variables.Snapshot();
        if (snapshot.Count == 0) return 0;

        if (_entries.Count == 0 || !_entries[^1].IsSeparator)
            AddSeparator();

        var now = DateTimeOffset.Now;
        foreach (var kv in snapshot.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            AddVariablePrint(kv.Key, kv.Value, now);

        AddSeparator();
        return snapshot.Count;
    }

    /// <summary>
    /// Apaga a última linha (cálculo, separador ou impressão de variável). As variáveis são recalculadas reavaliando o que sobrou,
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
            _entries.Add(old.IsStatic
                ? old
                : _evaluator.Evaluate(old.RawText, old.InputBase, old.Timestamp));
        }

        HistoryReplaced?.Invoke(this, EventArgs.Empty);
    }
}
