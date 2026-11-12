using System.Numerics;
using System.Text.Json.Serialization;
using CalculadoraHex.Core.Functions;
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

    /// <summary>Texto livre que começa por '#': inserido literalmente, sem avaliação.</summary>
    Comment,

    /// <summary>Resultado de Especiais > Raiz inteira: guarda variável, valor e n; o resto é recalculado.</summary>
    RootResult,
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

    [JsonIgnore]
    public bool IsComment => Kind == EntryKind.Comment;

    [JsonIgnore]
    public bool IsRootResult => Kind == EntryKind.RootResult;

    /// <summary>Quantidade de fatores n de uma raiz inteira (só em <see cref="EntryKind.RootResult"/>).</summary>
    public int? RootExponent { get; init; }

    [JsonIgnore]
    public IntegerRootResult? Root { get; init; }

    /// <summary>Sufixo numérico dos nomes das variáveis criadas pela raiz (1 = sem sufixo).</summary>
    public int? RootSuffix { get; init; }

    /// <summary>Variáveis que este resultado de raiz criou, com seus valores.</summary>
    [JsonIgnore]
    public IReadOnlyList<RootVariable> RootAssignments =>
        Root is null ? Array.Empty<RootVariable>() : RootVariables.Build(RawText, Root, RootSuffix ?? 1);

    /// <summary>Linhas que não são reavaliadas ao reconstruir o histórico (separadores, impressões e comentários).</summary>
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

    public static CalculationEntry CreateComment(string text, NumberBase inputBase, DateTimeOffset timestamp) => new()
    {
        Kind = EntryKind.Comment,
        Timestamp = timestamp,
        InputBase = inputBase,
        RawText = text,
    };

    public static CalculationEntry CreateRootResult(string variableName, IntegerRootResult result, DateTimeOffset timestamp, int suffix = 1) => new()
    {
        Kind = EntryKind.RootResult,
        Timestamp = timestamp,
        RawText = variableName,
        AssignedVariable = variableName,
        Value = result.Value,
        RootExponent = result.N,
        Root = result,
        RootSuffix = suffix,
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
