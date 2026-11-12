using System.Text;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Formatting;

/// <summary>
/// Reescreve uma linha já analisada com os números na base pedida. É o que permite mostrar
/// <c>a = a * 4321</c> (digitado em hexa) como <c>a = a * 17185</c> no painel decimal.
/// </summary>
public static class ExpressionPrinter
{
    public static string Print(Statement statement, NumberBase numberBase) =>
        Join(PrintSegments(statement, numberBase));

    public static string Print(Expression expression, NumberBase numberBase) =>
        Join(PrintSegments(expression, numberBase));

    /// <summary>Versão segmentada, para a UI colorir números, variáveis e tokens ambíguos.</summary>
    public static IReadOnlyList<TextSegment> PrintSegments(Statement statement, NumberBase numberBase)
    {
        ArgumentNullException.ThrowIfNull(statement);
        var segments = new List<TextSegment>();
        if (statement.AssignedVariable is not null)
        {
            segments.Add(new TextSegment(statement.AssignedVariable, SegmentKind.Variable));
            segments.Add(new TextSegment(" = ", SegmentKind.Punctuation));
        }
        Print(statement.Expression, numberBase, segments, parentPrecedence: 0);
        return segments;
    }

    public static IReadOnlyList<TextSegment> PrintSegments(Expression expression, NumberBase numberBase)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var segments = new List<TextSegment>();
        Print(expression, numberBase, segments, parentPrecedence: 0);
        return segments;
    }

    private static string Join(IReadOnlyList<TextSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var s in segments) sb.Append(s.Text);
        return sb.ToString();
    }

    private static void Print(Expression expression, NumberBase numberBase, List<TextSegment> segments, int parentPrecedence)
    {
        switch (expression)
        {
            case NumberLiteral n:
                segments.Add(new TextSegment(FormatLiteral(n, numberBase), SegmentKind.Number, n.Ambiguous));
                break;

            case VariableReference v:
                segments.Add(new TextSegment(v.Name, SegmentKind.Variable, v.Ambiguous));
                break;

            case FunctionCall f:
                segments.Add(new TextSegment(f.Name, SegmentKind.Operator));
                segments.Add(new TextSegment("(", SegmentKind.Punctuation));
                for (var i = 0; i < f.Arguments.Count; i++)
                {
                    if (i > 0) segments.Add(new TextSegment(", ", SegmentKind.Punctuation));
                    Print(f.Arguments[i], numberBase, segments, parentPrecedence: 0);
                }
                segments.Add(new TextSegment(")", SegmentKind.Punctuation));
                break;

            case NegateExpression neg:
                segments.Add(new TextSegment("-", SegmentKind.Operator));
                Print(neg.Operand, numberBase, segments, parentPrecedence: 3);
                break;

            case BinaryExpression b:
            {
                var precedence = Precedence(b.Operator);
                var needsParens = precedence < parentPrecedence;
                if (needsParens) segments.Add(new TextSegment("(", SegmentKind.Punctuation));
                Print(b.Left, numberBase, segments, precedence);
                segments.Add(new TextSegment($" {Symbol(b.Operator)} ", SegmentKind.Operator));
                // Lado direito com precedência +1 para preservar associatividade à esquerda (a - (b - c)).
                Print(b.Right, numberBase, segments, precedence + 1);
                if (needsParens) segments.Add(new TextSegment(")", SegmentKind.Punctuation));
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
    private static string FormatLiteral(NumberLiteral literal, NumberBase numberBase) =>
        NumberFormatter.FormatForEntry(literal.Value, numberBase);

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
