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
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>"hex" ou "dec".</summary>
    public string Base { get; set; } = "dec";

    public string Text { get; set; } = string.Empty;

    /// <summary>Valor decimal, apenas informativo (a linha é reavaliada ao carregar).</summary>
    public string? Value { get; set; }

    public string? Error { get; set; }
}
