using System.Numerics;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CalculadoraHex.Core.Parsing;

namespace CalculadoraHex.Core.Tests;

public class FunctionTests
{
    private static CalculationEntry Run(string text, NumberBase numberBase = NumberBase.Decimal, CalculationSession? session = null) =>
        (session ?? new CalculationSession()).Evaluate(text, numberBase);

    [Theory]
    [InlineData("sqrt(0)", 0)]
    [InlineData("sqrt(1)", 1)]
    [InlineData("sqrt(15)", 3)]
    [InlineData("sqrt(16)", 4)]
    [InlineData("sqrt(288261332929378871)", 536899741)]
    [InlineData("root(1234, 3)", 10)]
    [InlineData("root(1000, 3)", 10)]
    [InlineData("root(7, 1)", 7)]
    [InlineData("pow(10, 3)", 1000)]
    [InlineData("pow(7, 0)", 1)]
    [InlineData("SQRT(16)", 4)]
    public void Funcoes_calculam_com_inteiros(string linha, long esperado)
    {
        var e = Run(linha);

        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(new BigInteger(esperado), e.Value);
    }

    [Fact]
    public void Funcoes_entram_em_expressoes_e_aceitam_expressoes_como_argumento()
    {
        Assert.Equal(new BigInteger(7), Run("sqrt(3 * 3 + 7) + 3").Value);   // sqrt(16) + 3
        Assert.Equal(new BigInteger(-4), Run("-sqrt(16)").Value);
        Assert.Equal(new BigInteger(8), Run("pow(1 + 1, root(27, 3))").Value);
    }

    [Fact]
    public void Funcao_usa_variavel_e_a_atribuicao_guarda_o_resultado()
    {
        var s = new CalculationSession();
        s.Evaluate("v = 100", NumberBase.Decimal);

        var e = s.Evaluate("r = sqrt(v)", NumberBase.Decimal);

        Assert.True(e.Succeeded, e.Error);
        Assert.True(s.Variables.TryGet("r", out var r));
        Assert.Equal(new BigInteger(10), r);
    }

    [Fact]
    public void Em_hexa_os_argumentos_sao_lidos_em_hexa()
    {
        Assert.Equal(new BigInteger(0x10), Run("sqrt(100)", NumberBase.Hexadecimal).Value);   // 0x100 = 256
        Assert.Equal(new BigInteger(2), Run("root(0x40, 6)", NumberBase.Hexadecimal).Value);  // 64^(1/6)
    }

    [Fact]
    public void Linha_com_funcao_e_mostrada_nas_duas_bases()
    {
        var statement = Parser.Parse("root(a, 10)", NumberBase.Decimal, _ => true);

        Assert.Equal("root(a, 10)", ExpressionPrinter.Print(statement, NumberBase.Decimal));
        Assert.Equal("root(a, 0xA)", ExpressionPrinter.Print(statement, NumberBase.Hexadecimal));
    }

    [Theory]
    [InlineData("sqrt(-4)", "maiores ou iguais a zero")]
    [InlineData("root(10, 0)", "entre 1 e 10000")]
    [InlineData("root(10, 10001)", "entre 1 e 10000")]
    [InlineData("pow(2, -1)", "entre 0 e 10000")]
    [InlineData("pow(2, 10001)", "entre 0 e 10000")]
    [InlineData("pow(pow(10, 300), 10000)", "grande demais")]
    [InlineData("sqrt(1, 2)", "espera 1 argumento")]
    [InlineData("root(1)", "espera 2 argumentos")]
    [InlineData("foo(1)", "desconhecida")]
    [InlineData("sqrt(4", "Esperado ')'")]
    [InlineData("sqrt()", "inesperado")]
    public void Usos_invalidos_viram_erro_na_linha(string linha, string trecho)
    {
        var e = Run(linha);

        Assert.False(e.Succeeded);
        Assert.Contains(trecho, e.Error);
    }

    [Fact]
    public void Nome_de_funcao_sem_parenteses_continua_sendo_variavel()
    {
        var s = new CalculationSession();
        s.Evaluate("sqrt = 5", NumberBase.Decimal);

        Assert.Equal(new BigInteger(6), s.Evaluate("sqrt + 1", NumberBase.Decimal).Value);
        Assert.Equal(new BigInteger(4), s.Evaluate("sqrt(16)", NumberBase.Decimal).Value);
    }
}
