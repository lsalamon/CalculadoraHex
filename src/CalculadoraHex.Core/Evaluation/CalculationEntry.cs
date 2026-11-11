using System.Numerics;
using System.Text.Json.Serialization;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Evaluation;

public enum EntryKind
{
    /// <summary>Linha digitada e avaliada.</summary>
    Calculation,

    /// <summary>Linha tracejada inserida pelo usuário para separar blocos do histórico.</summary>
    Separator,

    /// <summary>Linha "nome = valor" gerada por Variáveis > Imprimir no display; guarda o valor daquele momento.</summary>
    VariablePrint,
}

/// <summary>
/// Uma linha do histórico. É única e independente da base: cada painel a renderiza na sua própria base.
/// </summary>
public sealed class CalculationEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    public EntryKind Kind { get; init; } = EntryKind.Calculation;

    /// <summary>Base em que o usuário digitou a linha.</summary>
    public NumberBase InputBase { get; init; }

    /// <summary>Texto exatamente como digitado (vazio em separadores).</summary>
    public required string RawText { get; init; }

    public string? AssignedVariable { get; init; }

    public BigInteger? Value { get; init; }

    public string? Error { get; init; }

    /// <summary>Árvore já resolvida (número × variável). Não é persistida: ao carregar, a linha é reavaliada.</summary>
    [JsonIgnore]
    public Statement? Statement { get; init; }

    [JsonIgnore]
    public bool IsSeparator => Kind == EntryKind.Separator;

    [JsonIgnore]
    public bool IsVariablePrint => Kind == EntryKind.VariablePrint;

    /// <summary>Linhas que não são reavaliadas ao reconstruir o histórico (separadores e impressões).</summary>
    [JsonIgnore]
    public bool IsStatic => Kind != EntryKind.Calculation;

    [JsonIgnore]
    public bool Succeeded => Kind == EntryKind.Calculation && Error is null && Value.HasValue;

    public static CalculationEntry CreateSeparator(DateTimeOffset timestamp) => new()
    {
        Kind = EntryKind.Separator,
        Timestamp = timestamp,
        RawText = string.Empty,
    };

    public static CalculationEntry CreateVariablePrint(string name, BigInteger value, DateTimeOffset timestamp) => new()
    {
        Kind = EntryKind.VariablePrint,
        Timestamp = timestamp,
        RawText = name,
        AssignedVariable = name,
        Value = value,
    };
}
