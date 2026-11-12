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

    // v1 = 536899741² + 1043511790, o exemplo da especificação
    private static readonly BigInteger V1 = BigInteger.Parse("288261332929378871");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Linhas_em_decimal_seguem_o_exemplo_da_especificacao()
    {
        var lines = RootScript.Build("v1", IntegerRoot.Compute(V1, 2), 1, NumberBase.Decimal);

        Assert.Equal(
            new[]
            {
                "#calcula raiz",
                "v1_raiz = sqrt(v1)",
                "#calcula o resultado",
                "v1_result = v1_raiz * v1_raiz",
                "#calcula a diferença",
                "v1_dif = v1 - v1_result",
            },
            lines);
    }

    [Fact]
    public void Com_n_diferente_de_dois_a_raiz_e_chamada_com_root_e_os_fatores_se_repetem()
    {
        var lines = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Decimal);

        Assert.Equal("a_raiz = root(a, 3)", lines[1]);
        Assert.Equal("a_result = a_raiz * a_raiz * a_raiz", lines[3]);
        Assert.Equal("a_dif = a - a_result", lines[5]);
    }

    [Fact]
    public void Acima_do_limite_de_fatores_o_resultado_usa_pow()
    {
        var seis = RootScript.Build("a", IntegerRoot.Compute(1000000, 6), 1, NumberBase.Decimal);
        var sete = RootScript.Build("a", IntegerRoot.Compute(10000000, 7), 1, NumberBase.Decimal);

        Assert.Equal("a_result = a_raiz * a_raiz * a_raiz * a_raiz * a_raiz * a_raiz", seis[3]);
        Assert.Equal("a_result = pow(a_raiz, 7)", sete[3]);
    }

    [Fact]
    public void Com_um_fator_o_resultado_e_a_propria_raiz()
    {
        var lines = RootScript.Build("v", IntegerRoot.Compute(7, 1), 1, NumberBase.Decimal);

        Assert.Equal("v_raiz = root(v, 1)", lines[1]);
        Assert.Equal("v_result = v_raiz", lines[3]);
    }

    [Fact]
    public void Conta_exata_tem_diferenca_zero()
    {
        var lines = RootScript.Build("b", IntegerRoot.Compute(1000, 3), 1, NumberBase.Decimal);

        Assert.Equal("b_dif = b - b_result", lines[5]);
    }

    [Fact]
    public void Sufixo_aparece_nos_nomes_das_variaveis()
    {
        var lines = RootScript.Build("a", IntegerRoot.Compute(1234, 2), 2, NumberBase.Decimal);

        Assert.Equal("a_raiz2 = sqrt(a)", lines[1]);
        Assert.Equal("a_result2 = a_raiz2 * a_raiz2", lines[3]);
        Assert.Equal("a_dif2 = a - a_result2", lines[5]);
    }

    [Fact]
    public void Em_hexa_so_o_n_muda_e_as_demais_linhas_sao_iguais_as_do_decimal()
    {
        // n = 10 é A em hexa, e como começa por letra leva 0x
        var dec = RootScript.Build("a", IntegerRoot.Compute(1234, 10), 1, NumberBase.Decimal);
        var hex = RootScript.Build("a", IntegerRoot.Compute(1234, 10), 1, NumberBase.Hexadecimal);

        Assert.Equal("a_raiz = root(a, 10)", dec[1]);
        Assert.Equal("a_raiz = root(a, 0xA)", hex[1]);
        Assert.Equal("a_result = pow(a_raiz, 10)", dec[3]);
        Assert.Equal("a_result = pow(a_raiz, 0xA)", hex[3]);

        var cubo = RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Hexadecimal);
        Assert.Equal(RootScript.Build("a", IntegerRoot.Compute(1234, 3), 1, NumberBase.Decimal), cubo);
    }

    [Theory]
    [InlineData(NumberBase.Decimal, 2)]
    [InlineData(NumberBase.Hexadecimal, 2)]
    [InlineData(NumberBase.Decimal, 3)]
    [InlineData(NumberBase.Hexadecimal, 3)]
    [InlineData(NumberBase.Decimal, 10)]
    [InlineData(NumberBase.Hexadecimal, 10)]
    public void Digitar_as_linhas_na_base_do_painel_cria_exatamente_os_valores_do_resultado(NumberBase typedBase, int n)
    {
        // Variáveis a..f existentes são a armadilha: sem o prefixo 0x, "A" seria lido como a variável "a".
        var session = new CalculationSession();
        foreach (var letter in "abcdef")
            session.Evaluate($"{letter} = 777", NumberBase.Decimal);
        session.Evaluate("fonte = 1234567890123", NumberBase.Decimal);

        var result = IntegerRoot.Compute(1234567890123, n);
        var suffix = RootVariables.ChooseSuffix("fonte", session.Variables.IsDefined);
        var names = RootVariables.Names("fonte", suffix);
        var entries = RootScript.Build("fonte", result, suffix, typedBase)
            .Select(line => session.Evaluate(line, typedBase))
            .ToList();

        Assert.All(entries, e => Assert.True(e.IsComment || e.Succeeded, $"'{e.RawText}' falhou: {e.Error}"));
        Assert.Equal(result.Root, entries[1].Value);                 // a chamada mostra a raiz
        Assert.True(session.Variables.TryGet(names.Root, out var root));
        Assert.Equal(result.Root, root);
        Assert.True(session.Variables.TryGet(names.Result, out var product));
        Assert.True(session.Variables.TryGet(names.Difference, out var difference));
        Assert.Equal(result.Power, product);
        Assert.Equal(result.Difference, difference);
    }

    [Fact]
    public void Linhas_digitadas_sao_entradas_comuns_que_sobrevivem_a_gravar_exportar_e_apagar()
    {
        var session = new CalculationSession();
        session.Evaluate("v1 = 288261332929378871", NumberBase.Decimal);
        foreach (var line in RootScript.Build("v1", IntegerRoot.Compute(V1, 2), 1, NumberBase.Decimal))
            session.Evaluate(line, NumberBase.Decimal);

        // 1 atribuição + 3 comentários + 3 atribuições
        Assert.Equal(7, session.Entries.Count);
        Assert.True(session.Entries[1].IsComment);
        Assert.Equal("#calcula raiz", session.Entries[1].RawText);
        Assert.Equal(new BigInteger(536899741), session.Entries[2].Value);
        Assert.Equal("v1_raiz", session.Entries[2].AssignedVariable);
        Assert.Equal(new BigInteger(1043511790), session.Entries[6].Value);

        var store = new SessionStore(_dir);
        var path = store.NewSessionPath();
        store.Save(session, path);
        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);
        Assert.Equal(7, loaded.Entries.Count);
        Assert.True(loaded.Variables.TryGet("v1_dif", out var dif));
        Assert.Equal(new BigInteger(1043511790), dif);

        var text = SessionStore.ExportText(session);
        Assert.Contains("v1_raiz = sqrt(v1)", text);
        var imported = new CalculationSession();
        var import = HistoryImporter.ImportInto(imported, HistoryImporter.ParseScript(text, NumberBase.Decimal));
        Assert.Equal(0, import.Failed);
        Assert.Equal(7, imported.Entries.Count);
        Assert.True(imported.Variables.TryGet("v1_raiz", out var raiz));
        Assert.Equal(new BigInteger(536899741), raiz);
        Assert.True(imported.Variables.TryGet("v1_result", out var product));
        Assert.Equal(BigInteger.Parse("288261331885867081"), product);

        // Ctrl+D apaga uma linha por vez, e a variável some com a sua linha
        session.RemoveLast();
        Assert.False(session.Variables.IsDefined("v1_dif"));
        Assert.True(session.Variables.IsDefined("v1_result"));
    }
}
