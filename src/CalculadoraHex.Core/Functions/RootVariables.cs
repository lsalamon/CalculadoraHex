using System.Globalization;

namespace CalculadoraHex.Core.Functions;

/// <summary>Os nomes das três variáveis novas que uma raiz inteira cria.</summary>
public sealed record RootNames(string Root, string Result, string Difference)
{
    public IEnumerable<string> All => new[] { Root, Result, Difference };
}

/// <summary>
/// Nomes das variáveis criadas por uma raiz inteira, derivados da variável de origem:
/// <c>v1_raiz</c> (a raiz), <c>v1_result</c> (a raiz multiplicada por si própria n vezes) e <c>v1_dif</c> (o que sobra de v1).
/// Se algum dos três já existe, o conjunto inteiro recebe um sufixo numérico (<c>v1_result2</c>, <c>v1_dif2</c>...),
/// de modo que as variáveis são sempre novas e nunca sobrescrevem as anteriores.
/// </summary>
public static class RootVariables
{
    public static RootNames Names(string source, int suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        var tail = suffix <= 1 ? string.Empty : suffix.ToString(CultureInfo.InvariantCulture);
        return new RootNames($"{source}_raiz{tail}", $"{source}_result{tail}", $"{source}_dif{tail}");
    }

    /// <summary>Menor sufixo (1 = sem sufixo) para o qual nenhum dos três nomes está definido.</summary>
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
