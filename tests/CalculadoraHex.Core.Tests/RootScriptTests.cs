using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CalculadoraHex.Core.Functions;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class RootScriptTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static List<string> Assignments(IEnumerable<string> lines) =>
        lines.Where(l => !l.StartsWith('#')).ToList();

    [Fact]
    public void Linhas_em_decimal_seguem_o_exemplo_da_especificacao_com_um_comentario_antes_de_cada_valor()
    {
        var lines = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Decimal);

        Assert.Equal(
            new[]
            {
                "#raiz 3 de a",
                "#a_raiz: x, o inteiro cuja potência de 3 fatores chega mais perto de a sem passar",
                "a_raiz = 10",
                "#a_pot: x^3, o produto de 3 fatores iguais a x",
                "a_pot = 1000",
                "#a_dif: a - x^3, o que sobra de a depois de tirar x^3 (zero se a conta é exata)",
                "a_dif = 234",
                "#a_prox: x + 1, o primeiro inteiro cuja potência passa de a",
                "a_prox = 11",
                "#a_proxpot: (x + 1)^3, a potência do inteiro seguinte",
                "a_proxpot = 1331",
                "#a_exc: (x + 1)^3 - a, quanto a potência do seguinte passa de a",
                "a_exc = 97",
            },
            lines);
    }

    [Fact]
    public void Todo_valor_vem_logo_depois_do_comentario_que_o_explica()
    {
        var lines = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Decimal);

        for (var i = 1; i < lines.Count; i += 2)
        {
            Assert.StartsWith("#", lines[i]);
            Assert.False(lines[i + 1].StartsWith('#'));
            var name = lines[i + 1].Split(' ')[0];
            Assert.StartsWith($"#{name}:", lines[i]);
        }
    }

    [Fact]
    public void Em_hexa_numero_que_comeca_por_letra_leva_0x_e_os_comentarios_nao_mudam()
    {
        var dec = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Decimal);
        var hex = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Hexadecimal);

        // 10 = A, 1000 = 3E8, 234 = EA, 11 = B, 1331 = 533, 97 = 61
        Assert.Equal(
            new[] { "a_raiz = 0xA", "a_pot = 3E8", "a_dif = 0xEA", "a_prox = 0xB", "a_proxpot = 533", "a_exc = 61" },
            Assignments(hex));
        Assert.Equal(
            dec.Where(l => l.StartsWith('#')),
            hex.Where(l => l.StartsWith('#')));
    }

    [Fact]
    public void Conta_exata_tem_cabecalho_e_tres_valores_comentados()
    {
        var lines = RootScript.Build("b", IntegerRoot.Compute(1000, 3), 1, NumberBase.Decimal);

        Assert.Equal(1 + 3 * 2, lines.Count);
        Assert.Equal(new[] { "b_raiz = 10", "b_pot = 1000", "b_dif = 0" }, Assignments(lines));
    }

    [Fact]
    public void Sufixo_aparece_nos_nomes_dos_valores_e_dos_comentarios()
    {
        var lines = RootScript.Build("a", IntegerRoot.Compute(1234, 2), 2, NumberBase.Decimal);

        Assert.Equal("a_raiz2 = 35", Assignments(lines)[0]);
        Assert.Equal("a_exc2 = 62", Assignments(lines)[5]);
        Assert.StartsWith("#a_raiz2:", lines[1]);
    }

    [Fact]
    public void Com_um_so_fator_o_texto_fica_no_singular()
    {
        var lines = RootScript.Build("v", IntegerRoot.Compute(7, 1), 1, NumberBase.Decimal);

        Assert.Contains("1 fator ", lines[1]);
        Assert.DoesNotContain("1 fatores", string.Join('\n', lines));
    }

    [Theory]
    [InlineData(NumberBase.Decimal)]
    [InlineData(NumberBase.Hexadecimal)]
    public void Digitar_as_linhas_na_base_do_painel_cria_exatamente_os_valores_do_resultado(NumberBase typedBase)
    {
        // Variáveis a..f existentes são a armadilha: sem o prefixo 0x, "A" seria lido como a variável "a".
        var session = new CalculationSession();
        foreach (var letter in "abcdef")
            session.Evaluate($"{letter} = 777", NumberBase.Decimal);
        session.Evaluate("fonte = 1234", NumberBase.Decimal);

        var result = IntegerRoot.Compute(1234, 3);
        var suffix = RootVariables.ChooseSuffix("fonte", result, session.Variables.IsDefined);
        foreach (var line in RootScript.Build("fonte", result, suffix, typedBase))
        {
            var entry = session.Evaluate(line, typedBase);
            Assert.True(entry.IsComment || entry.Succeeded, $"'{line}' falhou: {entry.Error}");
        }

        foreach (var expected in RootVariables.Build("fonte", result, suffix))
        {
            Assert.True(session.Variables.TryGet(expected.Name, out var actual), $"falta {expected.Name}");
            Assert.Equal(expected.Value, actual);
        }
    }

    [Fact]
    public void Formato_de_entrada_sempre_volta_o_mesmo_valor_nas_duas_bases()
    {
        var session = new CalculationSession();
        foreach (var letter in "abcdef")
            session.Evaluate($"{letter} = 999", NumberBase.Decimal);

        var values = Enumerable.Range(0, 600).Select(i => new BigInteger(i))
            .Concat(new[] { BigInteger.Pow(10, 40), BigInteger.Pow(16, 30) + 10, BigInteger.Pow(2, 200) - 1 });

        foreach (var value in values)
        {
            foreach (var numberBase in new[] { NumberBase.Decimal, NumberBase.Hexadecimal })
            {
                var text = NumberFormatter.FormatForEntry(value, numberBase);
                var entry = session.Evaluate($"x = {text}", numberBase);

                Assert.True(entry.Succeeded, $"'{text}' ({numberBase}) falhou: {entry.Error}");
                Assert.Equal(value, entry.Value);
            }
        }
    }

    [Fact]
    public void Linhas_digitadas_sao_entradas_comuns_que_sobrevivem_a_gravar_exportar_e_apagar()
    {
        var session = new CalculationSession();
        session.Evaluate("a = 1234", NumberBase.Decimal);
        var result = IntegerRoot.Compute(1234, 3);
        foreach (var line in RootScript.Build("a", result, 1, NumberBase.Decimal))
            session.Evaluate(line, NumberBase.Decimal);

        // 1 atribuição + cabeçalho + 6 comentários + 6 atribuições
        Assert.Equal(14, session.Entries.Count);
        Assert.True(session.Entries[1].IsComment);
        Assert.Equal("#raiz 3 de a", session.Entries[1].RawText);
        Assert.True(session.Entries[2].IsComment);
        Assert.StartsWith("#a_raiz:", session.Entries[2].RawText);

        var store = new SessionStore(_dir);
        var path = store.NewSessionPath();
        store.Save(session, path);
        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);
        Assert.Equal(14, loaded.Entries.Count);
        Assert.True(loaded.Variables.TryGet("a_exc", out var exc));
        Assert.Equal(new BigInteger(97), exc);

        var text = SessionStore.ExportText(session);
        Assert.Contains("#a_pot: x^3, o produto de 3 fatores iguais a x", text);

        var imported = new CalculationSession();
        var import = HistoryImporter.ImportInto(imported, HistoryImporter.ParseScript(text, NumberBase.Decimal));
        Assert.Equal(0, import.Failed);
        Assert.True(imported.Variables.TryGet("a_proxpot", out var proxpot));
        Assert.Equal(new BigInteger(1331), proxpot);
        Assert.Equal(14, imported.Entries.Count);

        // Ctrl+D apaga uma linha por vez, e a variável some com a sua linha
        session.RemoveLast();
        Assert.False(session.Variables.IsDefined("a_exc"));
        Assert.True(session.Variables.IsDefined("a_proxpot"));
    }
}
