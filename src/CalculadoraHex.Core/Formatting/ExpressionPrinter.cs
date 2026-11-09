using System.Text;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Formatting;

/// <summary>
/// Reescreve uma linha já analisada com os números na base pedida. É o que permite mostrar
/// <c>a = a * 4321</c> (digitado em hexa) como <c>a = a * 17185</c> no painel decimal.
/// </summary>
public static class ExpressionPrinter
{
    public static string Print(Statement statement, NumberBase numberBase)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var sb = new StringBuilder();
        if (statement.AssignedVariable is not null)
            sb.Append(statement.AssignedVariable).Append(" = ");
        Print(statement.Expression, numberBase, sb, parentPrecedence: 0);
        return sb.ToString();
    }

    public static string Print(Expression expression, NumberBase numberBase)
    {
        var sb = new StringBuilder();
        Print(expression, numberBase, sb, parentPrecedence: 0);
        return sb.ToString();
    }

    private static void Print(Expression expression, NumberBase numberBase, StringBuilder sb, int parentPrecedence)
    {
        switch (expression)
        {
            case NumberLiteral n:
                sb.Append(FormatLiteral(n, numberBase));
                break;

            case VariableReference v:
                sb.Append(v.Name);
                break;

            case NegateExpression neg:
                sb.Append('-');
                Print(neg.Operand, numberBase, sb, parentPrecedence: 3);
                break;

            case BinaryExpression b:
            {
                var precedence = Precedence(b.Operator);
                var needsParens = precedence < parentPrecedence;
                if (needsParens) sb.Append('(');
                Print(b.Left, numberBase, sb, precedence);
                sb.Append(' ').Append(Symbol(b.Operator)).Append(' ');
                // Lado direito com precedência +1 para preservar associatividade à esquerda (a - (b - c)).
                Print(b.Right, numberBase, sb, precedence + 1);
                if (needsParens) sb.Append(')');
                break;
            }

            default:
                throw new InvalidOperationException($"Nó desconhecido: {expression.GetType().Name}");
        }
    }

    /// <summary>
    /// Em hexa, um literal que começa por letra (ex.: <c>A0</c>) poderia ser confundido com um nome;
    /// nesse caso é impresso com prefixo <c>0x</c>.
    /// </summary>
    private static string FormatLiteral(NumberLiteral literal, NumberBase numberBase)
    {
        var text = NumberFormatter.Format(literal.Value, numberBase);
        if (numberBase == NumberBase.Hexadecimal)
        {
            var firstDigit = text.StartsWith('-') ? text[1..] : text;
            if (firstDigit.Length > 0 && char.IsLetter(firstDigit[0]))
                return text.StartsWith('-') ? "-0x" + firstDigit : "0x" + firstDigit;
        }
        return text;
    }

    private static int Precedence(BinaryOperator op) => op switch
    {
        BinaryOperator.Add or BinaryOperator.Subtract => 1,
        BinaryOperator.Multiply or BinaryOperator.Divide => 2,
        _ => 0,
    };

    private static char Symbol(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => '+',
        BinaryOperator.Subtract => '-',
        BinaryOperator.Multiply => '*',
        BinaryOperator.Divide => '/',
        _ => '?',
    };
}
