using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

/// <summary>O texto exportado precisa poder ser importado de volta (Arquivo > Importar histórico).</summary>
public class ExportImportRoundTripTests
{
    private static CalculationSession BuildSession()
    {
        var s = new CalculationSession();
        s.Evaluate("#soma de valores", NumberBase.Hexadecimal);
        s.Evaluate("a=1234+4321", NumberBase.Hexadecimal);
        s.Evaluate("b = a * 2", NumberBase.Decimal);
        s.Evaluate("1/0", NumberBase.Decimal);          // erro: não deve ser reproduzido na importação
        s.AddSeparator();
        s.PrintVariables();                             // a, b e separador final
        s.Evaluate("c = b + 1", NumberBase.Decimal);
        return s;
    }

    [Fact]
    public void Texto_exportado_importa_de_volta_o_mesmo_historico()
    {
        var original = BuildSession();
        var text = SessionStore.ExportText(original);

        var items = HistoryImporter.ParseScript(text, NumberBase.Decimal);
        var imported = new CalculationSession();
        var result = HistoryImporter.ImportInto(imported, items);

        Assert.Equal(0, result.Failed);
        Assert.Collection(imported.Entries,
            e => { Assert.True(e.IsComment); Assert.Equal("#soma de valores", e.RawText); },
            e => { Assert.Equal("a=1234+4321", e.RawText); Assert.Equal(NumberBase.Hexadecimal, e.InputBase); Assert.True(e.Succeeded); },
            e => { Assert.Equal("b = a * 2", e.RawText); Assert.Equal(NumberBase.Decimal, e.InputBase); },
            e => Assert.True(e.IsSeparator),
            e => { Assert.True(e.IsVariablePrint); Assert.Equal("a", e.RawText); Assert.Equal(new BigInteger(21845), e.Value); },
            e => { Assert.True(e.IsVariablePrint); Assert.Equal("b", e.RawText); Assert.Equal(new BigInteger(43690), e.Value); },
            e => Assert.True(e.IsSeparator),
            e => Assert.Equal("c = b + 1", e.RawText));

        Assert.True(imported.Variables.TryGet("c", out var c));
        Assert.Equal(new BigInteger(43691), c);
    }

    [Fact]
    public void Texto_exportado_traz_a_base_de_cada_linha_e_dispensa_perguntar_a_base()
    {
        var text = SessionStore.ExportText(BuildSession());

        var items = HistoryImporter.ParseScript(text, NumberBase.Decimal);

        Assert.NotEmpty(items);
        Assert.All(items, i => Assert.False(i.NeedsBase, $"'{i.Text}' ficaria sem base definida"));
    }

    [Fact]
    public void Texto_exportado_continua_legivel_nas_duas_bases()
    {
        var s = new CalculationSession();
        s.Evaluate("a=1234", NumberBase.Hexadecimal);
        s.Evaluate("a=a*4321", NumberBase.Hexadecimal);

        var text = SessionStore.ExportText(s);

        Assert.Contains("a = a * 4321", text);
        Assert.Contains("a = a * 17185", text);
        Assert.Contains("a = 4C5F4B4", text);
        Assert.Contains("a = 80082100", text);
    }

    [Fact]
    public void Script_ignora_comentario_no_fim_da_linha_mas_preserva_o_de_cerquilha()
    {
        var items = HistoryImporter.ParseScript("1+1   ; resultado: 2\n#nota; com ponto e vírgula\n[var] x = -7 ; hex: x = -7", NumberBase.Decimal);

        Assert.Collection(items,
            i => Assert.Equal("1+1", i.Text),
            i => Assert.Equal("#nota; com ponto e vírgula", i.Text),
            i => { Assert.True(i.IsVariablePrint); Assert.Equal("x", i.Text); Assert.Equal(new BigInteger(-7), i.PrintedValue); });
    }

    [Fact]
    public void Linha_var_malformada_vira_expressao_e_acusa_erro_na_importacao()
    {
        var items = HistoryImporter.ParseScript("[var] sem valor", NumberBase.Decimal);
        var s = new CalculationSession();

        var result = HistoryImporter.ImportInto(s, items);

        Assert.Equal(1, result.Imported);
        Assert.Equal(1, result.Failed);
    }

    [Fact]
    public void Script_manual_sem_prefixo_ainda_precisa_da_base_padrao()
    {
        var items = HistoryImporter.ParseScript("a = 10\n#nota\n---\n[dec] b = 1", NumberBase.Hexadecimal);

        Assert.True(items[0].NeedsBase);
        Assert.False(items[1].NeedsBase); // comentário
        Assert.False(items[2].NeedsBase); // separador
        Assert.False(items[3].NeedsBase); // base explícita
    }
}
