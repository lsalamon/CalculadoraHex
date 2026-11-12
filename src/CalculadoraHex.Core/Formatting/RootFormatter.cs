using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.Core.Formatting;

/// <param name="Title">Ex.: "raiz 3 de a = 1234".</param>
/// <param name="Detail">Ex.: "x = 10    x^3 = 1000    diferença 234" ou, se exato, "... exato".</param>
/// <param name="Note">Linha informativa com o inteiro seguinte; nula quando a conta é exata.</param>
public sealed record RootText(string Title, string Detail, string? Note);

/// <summary>Texto do resultado da raiz inteira, na base de cada painel. O expoente n é sempre decimal, por ser uma contagem.</summary>
public static class RootFormatter
{
    public static RootText Format(string variableName, IntegerRootResult result, NumberBase numberBase)
    {
        ArgumentNullException.ThrowIfNull(result);

        string F(System.Numerics.BigInteger value) => NumberFormatter.Format(value, numberBase);

        var title = $"raiz {result.N} de {variableName} = {F(result.Value)}";
        var detail = $"x = {F(result.Root)}    x^{result.N} = {F(result.Power)}    "
                     + (result.IsExact ? "exato" : $"diferença {F(result.Difference)}");
        var note = result.IsExact
            ? null
            : $"x = {F(result.NextRoot)} daria {F(result.NextPower)}, que passa {F(result.NextExcess)}";

        return new RootText(title, detail, note);
    }
}
