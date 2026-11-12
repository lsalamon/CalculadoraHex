using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculadoraHex.Core.Functions;

/// <summary>Os nomes das três variáveis novas que uma raiz inteira cria.</summary>
public sealed record RootNames(string Root, string Result, string Difference)
{
    public IEnumerable<string> All => new[] { Root, Result, Difference };
}

/// <summary>
/// Nomes das variáveis criadas por uma raiz inteira, derivados da variável de origem:
/// <c>v1_raiz1</c> (a raiz), <c>v1_result1</c> (a raiz multiplicada por si própria n vezes) e <c>v1_dif1</c> (o que sobra de v1).
/// Cada cálculo leva um número no fim dos três nomes (<c>v1_raiz1</c>, <c>v1_result1</c>, <c>v1_dif1</c>), que o distingue
/// dos demais: o primeiro cálculo usa 1 e, se algum dos três nomes já existe, o seguinte livre (2, 3...). Assim as
/// variáveis são sempre novas e nunca sobrescrevem as anteriores.
/// </summary>
public static partial class RootVariables
{
    [GeneratedRegex(@"^(?<base>.+)_(raiz|result|dif)\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex GeneratedName();

    /// <summary>
    /// A parte do nome que os novos nomes herdam. Se a origem já é uma variável criada por uma raiz
    /// (<c>v1_raiz1</c>), o que vale é <c>v1</c>: a raiz dela vira <c>v1_raiz2</c>, não <c>v1_raiz1_raiz1</c>.
    /// </summary>
    public static string BaseName(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var match = GeneratedName().Match(source);
        return match.Success ? match.Groups["base"].Value : source;
    }

    public static RootNames Names(string source, int suffix)
    {
        source = BaseName(source);

        var tail = suffix.ToString(CultureInfo.InvariantCulture);
        return new RootNames($"{source}_raiz{tail}", $"{source}_result{tail}", $"{source}_dif{tail}");
    }

    /// <summary>Menor número (a partir de 1) para o qual nenhum dos três nomes está definido.</summary>
    public static int ChooseSuffix(string source, Func<string, bool> isDefined)
    {
        ArgumentNullException.ThrowIfNull(isDefined);

        for (var suffix = 1; ; suffix++)
        {
            if (Names(source, suffix).All.All(name => !isDefined(name)))
                return suffix;
        }
    }
}
