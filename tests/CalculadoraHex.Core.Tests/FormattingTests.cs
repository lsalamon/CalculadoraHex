using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Tests;

public class FormattingTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(10, "A")]
    [InlineData(160, "A0")]
    [InlineData(255, "FF")]
    [InlineData(-255, "-FF")]
    [InlineData(4660, "1234")]
    [InlineData(80082100, "4C5F4B4")]
    public void Formata_hexa_sem_zero_a_esquerda_e_em_maiusculas(long valor, string esperado)
    {
        Assert.Equal(esperado, NumberFormatter.Format(new BigInteger(valor), NumberBase.Hexadecimal));
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(-42, "-42")]
    [InlineData(80082100, "80082100")]
    public void Formata_decimal(long valor, string esperado)
    {
        Assert.Equal(esperado, NumberFormatter.Format(new BigInteger(valor), NumberBase.Decimal));
    }

    [Fact]
    public void Reescreve_linha_hexa_em_decimal()
    {
        var evaluator = new Evaluator(new VariableStore());
        evaluator.Evaluate("a=1234", NumberBase.Hexadecimal);
        var e = evaluator.Evaluate("a=a * 4321", NumberBase.Hexadecimal);

        Assert.Equal("a = a * 4321", ExpressionPrinter.Print(e.Statement!, NumberBase.Hexadecimal));
        Assert.Equal("a = a * 17185", ExpressionPrinter.Print(e.Statement!, NumberBase.Decimal));
    }

    [Fact]
    public void Reescreve_linha_decimal_em_hexa_com_prefixo_quando_literal_comeca_por_letra()
    {
        var evaluator = new Evaluator(new VariableStore());
        var e = evaluator.Evaluate("x = 10 * 2 + 255", NumberBase.Decimal);

        Assert.Equal("x = 0xA * 2 + 0xFF", ExpressionPrinter.Print(e.Statement!, NumberBase.Hexadecimal));
    }

    [Theory]
    [InlineData("(2+3)*4", "(2 + 3) * 4")]
    [InlineData("2+3*4", "2 + 3 * 4")]
    [InlineData("10-(4-3)", "10 - (4 - 3)")]
    [InlineData("10-4-3", "10 - 4 - 3")]
    [InlineData("-(2+3)", "-(2 + 3)")]
    [InlineData("2*-3", "2 * -3")]
    public void Preserva_parenteses_necessarios(string entrada, string esperado)
    {
        var e = new Evaluator(new VariableStore()).Evaluate(entrada, NumberBase.Decimal);
        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(esperado, ExpressionPrinter.Print(e.Statement!, NumberBase.Decimal));
    }
}
