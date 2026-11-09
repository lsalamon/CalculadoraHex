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
