using System.Globalization;
using System.Numerics;

namespace CalculadoraHex.Core.Functions;

public sealed record RootVariable(string Name, BigInteger Value);

/// <summary>
/// Variáveis novas criadas por um resultado de raiz inteira, com nomes derivados da variável de origem:
/// <c>a_raiz</c> (x), <c>a_pot</c> (x^n), <c>a_dif</c> (diferença) e, se a conta não for exata,
/// <c>a_prox</c> (x + 1), <c>a_proxpot</c> ((x + 1)^n) e <c>a_exc</c> (quanto o seguinte passa).
/// Se algum desses nomes já existe, o conjunto inteiro recebe um sufixo numérico (<c>a_raiz2</c>, <c>a_pot2</c>...),
/// de modo que as variáveis são sempre novas e nunca sobrescrevem as anteriores.
/// </summary>
public static class RootVariables
{
    public static IReadOnlyList<RootVariable> Build(string source, IntegerRootResult result, int suffix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(result);

        var tail = suffix <= 1 ? string.Empty : suffix.ToString(CultureInfo.InvariantCulture);

        var list = new List<RootVariable>
        {
            new($"{source}_raiz{tail}", result.Root),
            new($"{source}_pot{tail}", result.Power),
            new($"{source}_dif{tail}", result.Difference),
        };

        if (!result.IsExact)
        {
            list.Add(new($"{source}_prox{tail}", result.NextRoot));
            list.Add(new($"{source}_proxpot{tail}", result.NextPower));
            list.Add(new($"{source}_exc{tail}", result.NextExcess));
        }

        return list;
    }

    /// <summary>Menor sufixo (1 = sem sufixo) para o qual nenhum dos nomes do conjunto está definido.</summary>
    public static int ChooseSuffix(string source, IntegerRootResult result, Func<string, bool> isDefined)
    {
        ArgumentNullException.ThrowIfNull(isDefined);

        for (var suffix = 1; ; suffix++)
        {
            if (Build(source, result, suffix).All(v => !isDefined(v.Name)))
                return suffix;
        }
    }
}
