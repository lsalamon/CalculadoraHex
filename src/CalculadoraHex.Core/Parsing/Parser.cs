using System.Globalization;
using System.Numerics;

namespace CalculadoraHex.Core.Parsing;

/// <summary>
/// Analisador descendente recursivo para a gramática:
/// <code>
/// linha := [ident '='] expr
/// expr  := termo (('+'|'-') termo)*
/// termo := fator (('*'|'/') fator)*
/// fator := numero | ident | '(' expr ')' | '-' fator
/// </code>
/// Os dígitos são interpretados na base informada. No painel hexa, uma palavra composta só de
/// dígitos hexa que começa por letra (ex.: <c>a</c>, <c>cafe</c>) é lida como variável se uma
/// variável com esse nome estiver definida; caso contrário, como número.
/// </summary>
public sealed class Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private readonly NumberBase _base;
    private readonly Func<string, bool> _isDefinedVariable;
    private int _index;

    private Parser(IReadOnlyList<Token> tokens, NumberBase numberBase, Func<string, bool> isDefinedVariable)
    {
        _tokens = tokens;
        _base = numberBase;
        _isDefinedVariable = isDefinedVariable;
    }

    public static Statement Parse(string text, NumberBase numberBase, Func<string, bool> isDefinedVariable)
    {
        ArgumentNullException.ThrowIfNull(isDefinedVariable);
        var tokens = Tokenizer.Tokenize(text);
        var parser = new Parser(tokens, numberBase, isDefinedVariable);
        return parser.ParseStatement();
    }

    private Token Current => _tokens[_index];

    private Token Advance() => _tokens[_index++];

    private Statement ParseStatement()
    {
        if (Current.Kind == TokenKind.End)
            throw new CalculationException("Linha vazia.", Current.Position);

        string? assigned = null;
        if (Current.Kind == TokenKind.Word && _tokens[_index + 1].Kind == TokenKind.Equals)
        {
            var nameToken = Advance();
            if (!IsIdentifier(nameToken.Text))
                throw new CalculationException($"'{nameToken.Text}' não é um nome de variável válido.", nameToken.Position);
            assigned = nameToken.Text;
            Advance(); // '='
        }

        var expression = ParseExpression();

        if (Current.Kind != TokenKind.End)
            throw new CalculationException($"Símbolo inesperado '{Current.Text}'.", Current.Position);

        return new Statement(assigned, expression);
    }

    private Expression ParseExpression()
    {
        var left = ParseTerm();
        while (Current.Kind is TokenKind.Plus or TokenKind.Minus)
        {
            var op = Advance().Kind == TokenKind.Plus ? BinaryOperator.Add : BinaryOperator.Subtract;
            var right = ParseTerm();
            left = new BinaryExpression(op, left, right);
        }
        return left;
    }

    private Expression ParseTerm()
    {
        var left = ParseFactor();
        while (Current.Kind is TokenKind.Star or TokenKind.Slash)
        {
            var op = Advance().Kind == TokenKind.Star ? BinaryOperator.Multiply : BinaryOperator.Divide;
            var right = ParseFactor();
            left = new BinaryExpression(op, left, right);
        }
        return left;
    }

    private Expression ParseFactor()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Minus:
                Advance();
                return new NegateExpression(ParseFactor());

            case TokenKind.LeftParen:
            {
                Advance();
                var inner = ParseExpression();
                if (Current.Kind != TokenKind.RightParen)
                    throw new CalculationException("Esperado ')'.", Current.Position);
                Advance();
                return inner;
            }

            case TokenKind.Word:
                Advance();
                return ClassifyWord(token);

            case TokenKind.End:
                throw new CalculationException("Expressão incompleta.", token.Position);

            default:
                throw new CalculationException($"Símbolo inesperado '{token.Text}'.", token.Position);
        }
    }

    private Expression ClassifyWord(Token token)
    {
        var text = token.Text;

        if (token.ForcedVariable)
        {
            if (!IsIdentifier(text))
                throw new CalculationException($"'{text}' não é um nome de variável válido.", token.Position);
            return new VariableReference(text);
        }

        return _base switch
        {
            NumberBase.Decimal => ClassifyDecimalWord(token),
            NumberBase.Hexadecimal => ClassifyHexWord(token),
            _ => throw new InvalidOperationException($"Base não suportada: {_base}"),
        };
    }

    private Expression ClassifyDecimalWord(Token token)
    {
        var text = token.Text;
        if (text.All(char.IsAsciiDigit))
            return new NumberLiteral(BigInteger.Parse(text, CultureInfo.InvariantCulture));

        if (IsIdentifier(text))
            return new VariableReference(text);

        throw new CalculationException($"'{text}' não é um número decimal nem um nome de variável válido.", token.Position);
    }

    private Expression ClassifyHexWord(Token token)
    {
        var text = token.Text;

        // 0x1F → número forçado
        if (text.Length > 2 && text[0] == '0' && (text[1] == 'x' || text[1] == 'X') && text[2..].All(char.IsAsciiHexDigit))
            return new NumberLiteral(ParseHex(text[2..]));

        // 1Fh → número forçado (estilo assembler; precisa começar por dígito)
        if (text.Length > 1 && (text[^1] == 'h' || text[^1] == 'H')
            && char.IsAsciiDigit(text[0]) && text[..^1].All(char.IsAsciiHexDigit))
            return new NumberLiteral(ParseHex(text[..^1]));

        if (text.All(char.IsAsciiHexDigit))
        {
            // Começa por dígito: nunca poderia ser nome → número.
            if (char.IsAsciiDigit(text[0]))
                return new NumberLiteral(ParseHex(text));

            // Começa por letra A-F: ambíguo. Variável definida vence.
            return _isDefinedVariable(text)
                ? new VariableReference(text, Ambiguous: true)
                : new NumberLiteral(ParseHex(text), Ambiguous: true);
        }

        if (IsIdentifier(text))
            return new VariableReference(text);

        throw new CalculationException($"'{text}' não é um número hexadecimal nem um nome de variável válido.", token.Position);
    }

    private static BigInteger ParseHex(string digits) =>
        // Prefixo '0' garante interpretação sem sinal.
        BigInteger.Parse("0" + digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);

    /// <summary>Nome válido: começa por letra ou '_' e segue com letras, dígitos ou '_'.</summary>
    public static bool IsIdentifier(string text) =>
        text.Length > 0
        && (char.IsLetter(text[0]) || text[0] == '_')
        && text.All(c => char.IsLetterOrDigit(c) || c == '_');
}
