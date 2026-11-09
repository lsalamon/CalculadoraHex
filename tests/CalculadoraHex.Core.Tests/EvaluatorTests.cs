using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Tests;

public class EvaluatorTests
{
    private readonly VariableStore _vars = new();
    private readonly Evaluator _evaluator;

    public EvaluatorTests() => _evaluator = new Evaluator(_vars);

    [Fact]
    public void Exemplo_do_enunciado_atribuicao_em_hexa_reflete_em_decimal()
    {
        var e1 = _evaluator.Evaluate("a=1234", NumberBase.Hexadecimal);

        Assert.True(e1.Succeeded, e1.Error);
        Assert.Equal("a", e1.AssignedVariable);
        Assert.Equal(new BigInteger(4660), e1.Value);
        Assert.Equal("1234", NumberFormatter.Format(e1.Value!.Value, NumberBase.Hexadecimal));
        Assert.Equal("4660", NumberFormatter.Format(e1.Value!.Value, NumberBase.Decimal));
    }

    [Fact]
    public void Exemplo_do_enunciado_a_igual_a_vezes_4321()
    {
        _evaluator.Evaluate("a=1234", NumberBase.Hexadecimal);
        var e2 = _evaluator.Evaluate("a=a * 4321", NumberBase.Hexadecimal);

        Assert.True(e2.Succeeded, e2.Error);
        Assert.Equal(new BigInteger(80082100), e2.Value);
        Assert.Equal("4C5F4B4", NumberFormatter.Format(e2.Value!.Value, NumberBase.Hexadecimal));
        Assert.Equal("80082100", NumberFormatter.Format(e2.Value!.Value, NumberBase.Decimal));
        Assert.True(_vars.TryGet("a", out var a));
        Assert.Equal(new BigInteger(80082100), a);
    }

    [Theory]
    [InlineData("2+3*4", 14)]
    [InlineData("(2+3)*4", 20)]
    [InlineData("10-4-3", 3)]
    [InlineData("-5+2", -3)]
    [InlineData("7/2", 3)]
    [InlineData("-7/2", -3)]
    [InlineData("2*-3", -6)]
    [InlineData("  42  ", 42)]
    public void Operacoes_decimais_basicas(string texto, long esperado)
    {
        var e = _evaluator.Evaluate(texto, NumberBase.Decimal);
        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(new BigInteger(esperado), e.Value);
    }

    [Theory]
    [InlineData("FF", 255)]
    [InlineData("ff", 255)]
    [InlineData("10", 16)]
    [InlineData("0xA0", 160)]
    [InlineData("1Fh", 31)]
    [InlineData("A+1", 11)]
    [InlineData("cafe", 51966)]
    public void Palavras_hexa_sem_variavel_definida_sao_numeros(string texto, long esperado)
    {
        var e = _evaluator.Evaluate(texto, NumberBase.Hexadecimal);
        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(new BigInteger(esperado), e.Value);
    }

    [Fact]
    public void Palavra_hexa_ambigua_vira_variavel_quando_definida()
    {
        _evaluator.Evaluate("a = 100", NumberBase.Decimal);
        var e = _evaluator.Evaluate("a+1", NumberBase.Hexadecimal);

        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(new BigInteger(101), e.Value);
    }

    [Fact]
    public void Prefixo_0x_forca_numero_mesmo_com_variavel_definida()
    {
        _evaluator.Evaluate("a = 100", NumberBase.Decimal);
        var e = _evaluator.Evaluate("0xa+1", NumberBase.Hexadecimal);

        Assert.True(e.Succeeded, e.Error);
        Assert.Equal(new BigInteger(11), e.Value);
    }

    [Fact]
    public void Prefixo_dolar_forca_variavel()
    {
        var e = _evaluator.Evaluate("$a", NumberBase.Hexadecimal);
        Assert.False(e.Succeeded);
        Assert.Contains("não definida", e.Error);
    }

    [Fact]
    public void Nomes_de_variavel_nao_diferenciam_maiusculas()
    {
        _evaluator.Evaluate("Total = 7", NumberBase.Decimal);
        var e = _evaluator.Evaluate("total * 2", NumberBase.Decimal);
        Assert.Equal(new BigInteger(14), e.Value);
    }

    [Fact]
    public void Entrada_decimal_formatada_em_hexa()
    {
        var e = _evaluator.Evaluate("b = 255 + 1", NumberBase.Decimal);
        Assert.Equal("100", NumberFormatter.Format(e.Value!.Value, NumberBase.Hexadecimal));
    }

    [Theory]
    [InlineData("1/0", "Divisão por zero")]
    [InlineData("x+1", "não definida")]
    [InlineData("2+", "incompleta")]
    [InlineData("(2+3", "Esperado ')'")]
    [InlineData("2 3", "inesperado")]
    [InlineData("2 # 3", "inválido")]
    [InlineData("1a", "não é um número decimal")]
    [InlineData("12 = 5", "nome de variável válido")]
    [InlineData("", "vazia")]
    public void Erros_sao_reportados_sem_lancar(string texto, string trecho)
    {
        var e = _evaluator.Evaluate(texto, NumberBase.Decimal);
        Assert.False(e.Succeeded);
        Assert.Contains(trecho, e.Error);
        Assert.Null(e.Value);
    }

    [Fact]
    public void Erro_nao_altera_variaveis()
    {
        _evaluator.Evaluate("a = 5", NumberBase.Decimal);
        _evaluator.Evaluate("a = 1/0", NumberBase.Decimal);
        Assert.True(_vars.TryGet("a", out var a));
        Assert.Equal(new BigInteger(5), a);
    }

    [Fact]
    public void Valores_grandes_nao_estouram()
    {
        var e = _evaluator.Evaluate("FFFFFFFFFFFFFFFF * FFFFFFFFFFFFFFFF", NumberBase.Hexadecimal);
        Assert.True(e.Succeeded, e.Error);
        Assert.Equal("FFFFFFFFFFFFFFFE0000000000000001", NumberFormatter.Format(e.Value!.Value, NumberBase.Hexadecimal));
    }
}
