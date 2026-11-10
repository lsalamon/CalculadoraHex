namespace CalculadoraHex.Core.Formatting;

public enum SegmentKind
{
    Number,
    Variable,
    Operator,
    Punctuation,
}

/// <summary>Pedaço de uma expressão impressa, com o papel de cada token para a UI poder realçá-lo.</summary>
/// <param name="Ambiguous">
/// Verdadeiro quando o token também poderia ser lido com outro sentido (número × variável) no painel hexa.
/// </param>
public sealed record TextSegment(string Text, SegmentKind Kind, bool Ambiguous = false);
