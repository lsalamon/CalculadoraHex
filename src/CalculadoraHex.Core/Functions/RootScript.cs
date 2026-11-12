using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Functions;

/// <summary>
/// As linhas que o resultado de uma raiz inteira "digita" na calculadora, como se o usuário fizesse a entrada dos dados:
/// <code>
/// #calcula raiz
/// v1_raiz1 = sqrt(v1)              (ou root(v1, n) quando n não é 2)
/// #calcula o resultado
/// v1_result1 = v1_raiz1 * v1_raiz1 (n fatores; acima de 6 fatores, pow(v1_raiz1, n))
/// #calcula a diferença
/// v1_dif1 = v1 - v1_result1
/// </code>
/// Todo valor que já é guardado numa variável aparece pelo nome dela, não pelo número. O único número escrito é o
/// n de root e pow, na base do painel em que as linhas serão digitadas. Os comentários são literais e valem igual
/// nos dois painéis.
/// </summary>
public static class RootScript
{
    /// <summary>Até esta quantidade de fatores a multiplicação é escrita por extenso; acima, usa-se pow.</summary>
    public const int MaxWrittenFactors = 6;

    public static IReadOnlyList<string> Build(string source, IntegerRootResult result, int suffix, NumberBase typedBase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(result);

        var names = RootVariables.Names(source, suffix);
        var exponent = NumberFormatter.FormatForEntry(result.N, typedBase);

        var rootCall = result.N == 2 ? $"sqrt({source})" : $"root({source}, {exponent})";
        var product = result.N <= MaxWrittenFactors
            ? string.Join(" * ", Enumerable.Repeat(names.Root, result.N))
            : $"pow({names.Root}, {exponent})";

        return new[]
        {
            "#calcula raiz",
            $"{names.Root} = {rootCall}",
            "#calcula o resultado",
            $"{names.Result} = {product}",
            "#calcula a diferença",
            $"{names.Difference} = {source} - {names.Result}",
        };
    }
}
