using System.Globalization;
using System.Numerics;

namespace CalculadoraHex.Core.Functions;

/// <param name="Explanation">O que o valor é, em uma frase; vai num comentário antes da atribuição.</param>
public sealed record RootVariable(string Name, BigInteger Value, string Explanation);

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
        var n = result.N;
        var factors = n == 1 ? "1 fator" : $"{n} fatores";

        var list = new List<RootVariable>
        {
            new($"{source}_raiz{tail}", result.Root,
                $"x, o inteiro cuja potência de {factors} chega mais perto de {source} sem passar"),
            new($"{source}_pot{tail}", result.Power,
                $"x^{n}, o produto de {factors} iguais a x"),
            new($"{source}_dif{tail}", result.Difference,
                $"{source} - x^{n}, o que sobra de {source} depois de tirar x^{n} (zero se a conta é exata)"),
        };

        if (!result.IsExact)
        {
            list.Add(new($"{source}_prox{tail}", result.NextRoot,
                $"x + 1, o primeiro inteiro cuja potência passa de {source}"));
            list.Add(new($"{source}_proxpot{tail}", result.NextPower,
                $"(x + 1)^{n}, a potência do inteiro seguinte"));
            list.Add(new($"{source}_exc{tail}", result.NextExcess,
                $"(x + 1)^{n} - {source}, quanto a potência do seguinte passa de {source}"));
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
