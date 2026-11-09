namespace CalculadoraHex.Core.Parsing;

/// <summary>
/// Quebra a linha em tokens. Não decide se uma palavra é número ou variável: isso depende da base
/// e das variáveis definidas, e fica a cargo do <see cref="Parser"/>.
/// </summary>
public static class Tokenizer
{
    public static IReadOnlyList<Token> Tokenize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokens = new List<Token>();
        var i = 0;

        while (i < text.Length)
        {
            var c = text[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (IsWordChar(c))
            {
                var start = i;
                while (i < text.Length && IsWordChar(text[i])) i++;
                tokens.Add(new Token(TokenKind.Word, text[start..i], start));
                continue;
            }

            if (c == '$')
            {
                var start = i;
                i++;
                var wordStart = i;
                while (i < text.Length && IsWordChar(text[i])) i++;
                if (i == wordStart)
                    throw new CalculationException("Esperado nome de variável após '$'.", start);
                tokens.Add(new Token(TokenKind.Word, text[wordStart..i], start, ForcedVariable: true));
                continue;
            }

            var kind = c switch
            {
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                '=' => TokenKind.Equals,
                _ => throw new CalculationException($"Caractere inválido '{c}'.", i),
            };

            tokens.Add(new Token(kind, c.ToString(), i));
            i++;
        }

        tokens.Add(new Token(TokenKind.End, string.Empty, text.Length));
        return tokens;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
}
