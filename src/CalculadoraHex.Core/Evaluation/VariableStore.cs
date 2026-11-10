using System.Numerics;

namespace CalculadoraHex.Core.Evaluation;

/// <summary>Variáveis nomeadas da sessão. Nomes não diferenciam maiúsculas de minúsculas.</summary>
public sealed class VariableStore
{
    private readonly SortedDictionary<string, BigInteger> _values = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler? Changed;

    public int Count => _values.Count;

    public IReadOnlyDictionary<string, BigInteger> Snapshot() =>
        new Dictionary<string, BigInteger>(_values, StringComparer.OrdinalIgnoreCase);

    public bool IsDefined(string name) => _values.ContainsKey(name);

    public bool TryGet(string name, out BigInteger value) => _values.TryGetValue(name, out value);

    public void Set(string name, BigInteger value)
    {
        _values[name] = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool Remove(string name)
    {
        var removed = _values.Remove(name);
        if (removed) Changed?.Invoke(this, EventArgs.Empty);
        return removed;
    }

    public void Clear()
    {
        if (_values.Count == 0) return;
        _values.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Substitui todo o conjunto de variáveis (usado ao carregar uma sessão gravada).</summary>
    public void ReplaceAll(IReadOnlyDictionary<string, BigInteger> values)
    {
        _values.Clear();
        foreach (var kv in values) _values[kv.Key] = kv.Value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
