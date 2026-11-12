using System.Globalization;
using System.Numerics;

namespace CalculadoraHex.Core.Formatting;

public static class NumberFormatter
{
    /// <summary>Formata um valor na base pedida, sem prefixo. Hexa em maiúsculas; negativos com '-'.</summary>
    public static string Format(BigInteger value, NumberBase numberBase) => numberBase switch
    {
        NumberBase.Decimal => value.ToString(CultureInfo.InvariantCulture),
        NumberBase.Hexadecimal => FormatHex(value),
        _ => throw new ArgumentOutOfRangeException(nameof(numberBase)),
    };

    /// <summary>
    /// Formata um valor para ser digitado na calculadora. Em hexa, um número que começa por letra (A a F) recebe o prefixo
    /// <c>0x</c>, porque sem ele a palavra seria lida como variável quando existe uma com esse nome (ex.: <c>a</c>).
    /// Digitar o resultado na mesma base devolve sempre o mesmo valor.
    /// </summary>
    public static string FormatForEntry(BigInteger value, NumberBase numberBase)
    {
        var text = Format(value, numberBase);
        if (numberBase != NumberBase.Hexadecimal) return text;

        var negative = text.StartsWith('-');
        var digits = negative ? text[1..] : text;
        return digits.Length > 0 && char.IsLetter(digits[0])
            ? (negative ? "-0x" : "0x") + digits
            : text;
    }

    private static string FormatHex(BigInteger value)
    {
        if (value.IsZero) return "0";

        var magnitude = BigInteger.Abs(value);
        // ToString("X") pode incluir um '0' à esquerda para evitar leitura com sinal; removemos.
        var digits = magnitude.ToString("X", CultureInfo.InvariantCulture).TrimStart('0');
        if (digits.Length == 0) digits = "0";

        return value.Sign < 0 ? "-" + digits : digits;
    }
}
