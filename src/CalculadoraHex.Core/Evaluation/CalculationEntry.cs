using System.Numerics;
using System.Text.Json.Serialization;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Evaluation;

/// <summary>
/// Uma linha do histórico. É única e independente da base: cada painel a renderiza na sua própria base.
/// </summary>
public sealed class CalculationEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;

    /// <summary>Base em que o usuário digitou a linha.</summary>
    public NumberBase InputBase { get; init; }

    /// <summary>Texto exatamente como digitado.</summary>
    public required string RawText { get; init; }

    public string? AssignedVariable { get; init; }

    public BigInteger? Value { get; init; }

    public string? Error { get; init; }

    /// <summary>Árvore já resolvida (número × variável). Não é persistida: ao carregar, a linha é reavaliada.</summary>
    [JsonIgnore]
    public Statement? Statement { get; init; }

    [JsonIgnore]
    public bool Succeeded => Error is null && Value.HasValue;
}
