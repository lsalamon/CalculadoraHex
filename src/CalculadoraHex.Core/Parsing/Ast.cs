using System.Numerics;

namespace CalculadoraHex.Core.Parsing;

public abstract record Expression;

/// <param name="Ambiguous">
/// Verdadeiro quando o texto digitado também poderia ser lido como variável (ex.: <c>a</c> no painel hexa)
/// e a regra de desambiguação escolheu número.
/// </param>
public sealed record NumberLiteral(BigInteger Value, bool Ambiguous = false) : Expression;

/// <param name="Ambiguous">
/// Verdadeiro quando o texto digitado também poderia ser lido como número hexa (ex.: <c>a</c>)
/// e a regra de desambiguação escolheu variável.
/// </param>
public sealed record VariableReference(string Name, bool Ambiguous = false) : Expression;

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

public sealed record BinaryExpression(BinaryOperator Operator, Expression Left, Expression Right) : Expression;

public sealed record NegateExpression(Expression Operand) : Expression;

/// <summary>Chamada de função: <c>sqrt(x)</c>, <c>root(x, n)</c>, <c>pow(x, n)</c>.</summary>
public sealed record FunctionCall(string Name, IReadOnlyList<Expression> Arguments) : Expression;

/// <summary>Uma linha completa: expressão opcionalmente atribuída a uma variável.</summary>
public sealed record Statement(string? AssignedVariable, Expression Expression)
{
    public bool IsAssignment => AssignedVariable is not null;
}
