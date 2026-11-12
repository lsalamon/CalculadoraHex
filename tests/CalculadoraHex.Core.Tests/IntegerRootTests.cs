using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.Core.Tests;

public class IntegerRootTests
{
    [Fact]
    public void Conta_exata_nao_tem_diferenca()
    {
        var r = IntegerRoot.Compute(1000, 3);

        Assert.Equal(new BigInteger(10), r.Root);
        Assert.Equal(new BigInteger(1000), r.Power);
        Assert.Equal(BigInteger.Zero, r.Difference);
        Assert.True(r.IsExact);
    }

    [Fact]
    public void Conta_inexata_informa_a_diferenca()
    {
        var r = IntegerRoot.Compute(1234, 3);

        Assert.Equal(new BigInteger(10), r.Root);
        Assert.Equal(new BigInteger(1000), r.Power);
        Assert.Equal(new BigInteger(234), r.Difference);
        Assert.False(r.IsExact);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(1, 5, 1)]
    [InlineData(7, 1, 7)]       // n = 1: o próprio valor
    [InlineData(2, 2, 1)]
    [InlineData(4, 2, 2)]
    [InlineData(5, 10, 1)]      // n maior que o número de bits: a raiz é 1
    [InlineData(99, 10000, 1)]
    public void Casos_de_borda(long valor, int n, long raiz)
    {
        var r = IntegerRoot.Compute(valor, n);

        Assert.Equal(new BigInteger(raiz), r.Root);
        Assert.Equal(new BigInteger(valor) - BigInteger.Pow(raiz, n), r.Difference);
    }

    [Fact]
    public void Raiz_confere_com_forca_bruta_para_muitos_valores_e_expoentes()
    {
        for (var n = 1; n <= 12; n++)
        {
            for (var v = 0; v <= 3000; v++)
            {
                var r = IntegerRoot.Compute(v, n);

                Assert.True(BigInteger.Pow(r.Root, n) <= v, $"{r.Root}^{n} passou de {v}");
                Assert.True(BigInteger.Pow(r.Root + 1, n) > v, $"{r.Root + 1}^{n} não passou de {v}");
                Assert.Equal(v - BigInteger.Pow(r.Root, n), r.Difference);
                Assert.Equal(r.Difference.IsZero, r.IsExact);
            }
        }
    }

    [Fact]
    public void Numeros_enormes_sao_exatos()
    {
        var cem = BigInteger.Pow(10, 100);

        var exato = IntegerRoot.Compute(cem, 2);
        Assert.Equal(BigInteger.Pow(10, 50), exato.Root);
        Assert.True(exato.IsExact);

        var inexato = IntegerRoot.Compute(cem + 1, 2);
        Assert.Equal(BigInteger.Pow(10, 50), inexato.Root);
        Assert.Equal(BigInteger.One, inexato.Difference);

        var grande = BigInteger.Pow(3, 500) * 7 + 12345;
        var r = IntegerRoot.Compute(grande, 7);
        Assert.True(BigInteger.Pow(r.Root, 7) <= grande);
        Assert.True(BigInteger.Pow(r.Root + 1, 7) > grande);

        var gigante = BigInteger.Pow(2, 20000);
        Assert.Equal(BigInteger.Pow(2, 10000), IntegerRoot.Compute(gigante, 2).Root);
    }

    [Theory]
    [InlineData(-1, 2, "maiores ou iguais a zero")]
    [InlineData(-1000, 3, "maiores ou iguais a zero")]
    [InlineData(10, 0, "entre 1 e 10000")]
    [InlineData(10, -3, "entre 1 e 10000")]
    [InlineData(10, 10001, "entre 1 e 10000")]
    public void Argumentos_invalidos_geram_mensagem_para_o_usuario(long valor, int n, string trecho)
    {
        var ex = Assert.Throws<CalculationException>(() => IntegerRoot.Compute(valor, n));
        Assert.Contains(trecho, ex.Message);
    }
}
