namespace CalculadoraHex.Core.Parsing;

public enum TokenKind
{
    /// <summary>Sequência alfanumérica: pode ser número ou nome de variável, conforme a base.</summary>
    Word,
    Plus,
    Minus,
    Star,
    Slash,
    LeftParen,
    RightParen,
    Comma,
    Equals,
    End,
}

/// <param name="ForcedVariable">Verdadeiro quando a palavra veio prefixada por <c>$</c>, forçando leitura como variável.</param>
public readonly record struct Token(TokenKind Kind, string Text, int Position, bool ForcedVariable = false)
{
    public override string ToString() => Kind == TokenKind.Word ? $"{Kind}('{Text}')" : Kind.ToString();
}
