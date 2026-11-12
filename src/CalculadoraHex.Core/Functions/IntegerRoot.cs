using System.Numerics;

namespace CalculadoraHex.Core.Functions;

/// <param name="Value">Valor perseguido (o que a variável guarda).</param>
/// <param name="N">Quantidade de fatores: x multiplicado por si próprio, com n fatores, é x^n.</param>
/// <param name="Root">Maior inteiro x tal que x^n não passa de <paramref name="Value"/> (o menor dos dois inteiros que cercam a raiz).</param>
/// <param name="Power">x^n para x = <paramref name="Root"/>.</param>
/// <param name="Difference">Value menos Power; zero quando a conta é exata.</param>
/// <param name="NextRoot">Inteiro seguinte, Root + 1.</param>
/// <param name="NextPower">NextRoot^n, sempre maior que Value quando a conta não é exata.</param>
/// <param name="NextExcess">NextPower menos Value: quanto o inteiro seguinte passa.</param>
public sealed record IntegerRootResult(
    BigInteger Value,
    int N,
    BigInteger Root,
    BigInteger Power,
    BigInteger Difference,
    bool IsExact,
    BigInteger NextRoot,
    BigInteger NextPower,
    BigInteger NextExcess);

/// <summary>
/// Raiz n-ésima inteira: o menor inteiro que, multiplicado por si próprio n vezes (n fatores),
/// chega a um valor sem passar dele, junto com a diferença quando a conta não fecha.
/// Tudo em inteiros de precisão arbitrária, sem ponto flutuante.
/// </summary>
public static class IntegerRoot
{
    public const int MinExponent = 1;

    public const int MaxExponent = 10000;

    /// <summary>Confere os argumentos sem calcular nada. Lança <see cref="CalculationException"/> com mensagem para o usuário.</summary>
    public static void Validate(BigInteger value, int n)
    {
        if (n < MinExponent || n > MaxExponent)
            throw new CalculationException($"O valor de n deve ser um inteiro entre {MinExponent} e {MaxExponent}.");

        if (value.Sign < 0)
            throw new CalculationException(
                $"Só é possível calcular a raiz de valores maiores ou iguais a zero. Esta variável vale {value}.");
    }

    public static IntegerRootResult Compute(BigInteger value, int n)
    {
        Validate(value, n);

        var root = FloorRoot(value, n);
        var power = BigInteger.Pow(root, n);
        var next = root + 1;
        var nextPower = BigInteger.Pow(next, n);

        return new IntegerRootResult(
            Value: value,
            N: n,
            Root: root,
            Power: power,
            Difference: value - power,
            IsExact: power == value,
            NextRoot: next,
            NextPower: nextPower,
            NextExcess: nextPower - value);
    }

    /// <summary>Maior x com x^n &lt;= value, por Newton em inteiros partindo de um limite superior.</summary>
    private static BigInteger FloorRoot(BigInteger value, int n)
    {
        if (n == 1 || value < 2) return value; // 0 e 1 valem para qualquer n

        // 2^(bits/n + 1) elevado a n é maior que 2^bits, logo maior que value: serve de ponto de partida.
        var bits = (long)value.GetBitLength();
        var x = BigInteger.One << (int)(bits / n + 1);

        while (true)
        {
            var y = ((n - 1) * x + value / BigInteger.Pow(x, n - 1)) / n;
            if (y >= x) return x;
            x = y;
        }
    }
}
