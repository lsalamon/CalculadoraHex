using System.Text.Json.Serialization;

namespace CalculadoraHex.Core.Persistence;

/// <summary>Forma gravada em JSON de uma sessão. Os valores são strings para não depender de BigInteger no serializador.</summary>
public sealed class SessionDocument
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<EntryDocument> Entries { get; set; } = new();

    /// <summary>Snapshot final das variáveis (nome → valor decimal). Prevalece sobre a re-execução das linhas.</summary>
    public Dictionary<string, string> Variables { get; set; } = new();
}

public sealed class EntryDocument
{
    public const string SeparatorKind = "sep";

    public const string VariablePrintKind = "var";

    public DateTimeOffset Timestamp { get; set; }

    /// <summary>"sep" para linha separadora, "var" para impressão de variável; ausente ou nulo para cálculo.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; set; }

    /// <summary>"hex" ou "dec".</summary>
    public string Base { get; set; } = "dec";

    public string Text { get; set; } = string.Empty;

    /// <summary>Valor decimal, apenas informativo (a linha é reavaliada ao carregar).</summary>
    public string? Value { get; set; }

    public string? Error { get; set; }

    [JsonIgnore]
    public bool IsSeparator => string.Equals(Kind, SeparatorKind, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsVariablePrint => string.Equals(Kind, VariablePrintKind, StringComparison.OrdinalIgnoreCase);
}
