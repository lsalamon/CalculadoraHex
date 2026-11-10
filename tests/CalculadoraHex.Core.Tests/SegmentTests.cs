using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Tests;

public class SegmentTests
{
    [Fact]
    public void Marca_variavel_ambigua_no_painel_hexa()
    {
        var evaluator = new Evaluator(new VariableStore());
        evaluator.Evaluate("a=1234", NumberBase.Hexadecimal);
        var e = evaluator.Evaluate("a=a*4321", NumberBase.Hexadecimal);

        var segments = ExpressionPrinter.PrintSegments(e.Statement!, NumberBase.Hexadecimal);

        Assert.Collection(segments,
            s => { Assert.Equal("a", s.Text); Assert.Equal(SegmentKind.Variable, s.Kind); Assert.False(s.Ambiguous); },
            s => { Assert.Equal(" = ", s.Text); Assert.Equal(SegmentKind.Punctuation, s.Kind); },
            s => { Assert.Equal("a", s.Text); Assert.Equal(SegmentKind.Variable, s.Kind); Assert.True(s.Ambiguous); },
            s => { Assert.Equal(" * ", s.Text); Assert.Equal(SegmentKind.Operator, s.Kind); },
            s => { Assert.Equal("4321", s.Text); Assert.Equal(SegmentKind.Number, s.Kind); Assert.False(s.Ambiguous); });
    }

    [Fact]
    public void Marca_numero_ambiguo_quando_variavel_nao_existe()
    {
        var e = new Evaluator(new VariableStore()).Evaluate("cafe + 1", NumberBase.Hexadecimal);

        var segments = ExpressionPrinter.PrintSegments(e.Statement!, NumberBase.Hexadecimal);

        Assert.Equal(SegmentKind.Number, segments[0].Kind);
        Assert.True(segments[0].Ambiguous);
        Assert.Equal("0xCAFE", segments[0].Text);
        Assert.Equal("0xCAFE + 1", ExpressionPrinter.Print(e.Statement!, NumberBase.Hexadecimal));
    }

    [Fact]
    public void Segmentos_concatenados_igualam_o_texto_impresso()
    {
        var e = new Evaluator(new VariableStore()).Evaluate("x = -(2+3)*4", NumberBase.Decimal);

        var text = ExpressionPrinter.Print(e.Statement!, NumberBase.Decimal);
        var joined = string.Concat(ExpressionPrinter.PrintSegments(e.Statement!, NumberBase.Decimal).Select(s => s.Text));

        Assert.Equal(text, joined);
        Assert.Equal("x = -(2 + 3) * 4", text);
    }
}
