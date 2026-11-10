using System.Numerics;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Evaluation;

/// <summary>Avalia uma linha digitada e atualiza as variáveis. Nunca lança: erros vão para <see cref="CalculationEntry.Error"/>.</summary>
public sealed class Evaluator
{
    public Evaluator(VariableStore variables)
    {
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
    }

    public VariableStore Variables { get; }

    public CalculationEntry Evaluate(string text, NumberBase inputBase) =>
        Evaluate(text, inputBase, DateTimeOffset.Now);

    /// <param name="timestamp">Momento original da linha (preservado ao recarregar uma sessão gravada).</param>
    public CalculationEntry Evaluate(string text, NumberBase inputBase, DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(text);

        try
        {
            var statement = Parser.Parse(text, inputBase, Variables.IsDefined);
            var value = Evaluate(statement.Expression);

            if (statement.AssignedVariable is not null)
                Variables.Set(statement.AssignedVariable, value);

            return new CalculationEntry
            {
                Timestamp = timestamp,
                InputBase = inputBase,
                RawText = text,
                AssignedVariable = statement.AssignedVariable,
                Value = value,
                Statement = statement,
            };
        }
        catch (CalculationException ex)
        {
            return new CalculationEntry
            {
                Timestamp = timestamp,
                InputBase = inputBase,
                RawText = text,
                Error = ex.Message,
            };
        }
    }

    private BigInteger Evaluate(Expression expression) => expression switch
    {
        NumberLiteral n => n.Value,
        VariableReference v => Variables.TryGet(v.Name, out var value)
            ? value
            : throw new CalculationException($"Variável '{v.Name}' não definida."),
        NegateExpression neg => -Evaluate(neg.Operand),
        BinaryExpression b => EvaluateBinary(b),
        _ => throw new InvalidOperationException($"Nó desconhecido: {expression.GetType().Name}"),
    };

    private BigInteger EvaluateBinary(BinaryExpression b)
    {
        var left = Evaluate(b.Left);
        var right = Evaluate(b.Right);
        return b.Operator switch
        {
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => right.IsZero
                ? throw new CalculationException("Divisão por zero.")
                : BigInteger.Divide(left, right), // divisão inteira, truncada em direção a zero
            _ => throw new InvalidOperationException($"Operador desconhecido: {b.Operator}"),
        };
    }
}
