using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class PrintVariablesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Imprime_variaveis_em_ordem_entre_separadores()
    {
        var s = new CalculationSession();
        s.Evaluate("b = 2", NumberBase.Decimal);
        s.Evaluate("A = 1", NumberBase.Decimal);

        var count = s.PrintVariables();

        Assert.Equal(2, count);
        Assert.Equal(6, s.Entries.Count);
        Assert.True(s.Entries[2].IsSeparator);
        Assert.True(s.Entries[3].IsVariablePrint);
        Assert.Equal("A", s.Entries[3].RawText);
        Assert.Equal(new BigInteger(1), s.Entries[3].Value);
        Assert.Equal("b", s.Entries[4].RawText);
        Assert.True(s.Entries[5].IsSeparator);
    }

    [Fact]
    public void Nao_repete_separador_se_a_ultima_linha_ja_for_um()
    {
        var s = new CalculationSession();
        s.Evaluate("x = 1", NumberBase.Decimal);
        s.AddSeparator();

        s.PrintVariables();

        Assert.Equal(4, s.Entries.Count); // x=1, sep, x, sep
        Assert.True(s.Entries[1].IsSeparator);
        Assert.True(s.Entries[2].IsVariablePrint);
        Assert.True(s.Entries[3].IsSeparator);
    }

    [Fact]
    public void Sem_variaveis_nao_imprime_nada()
    {
        var s = new CalculationSession();
        s.Evaluate("1+1", NumberBase.Decimal);

        Assert.Equal(0, s.PrintVariables());
        Assert.Single(s.Entries);
    }

    [Fact]
    public void Impressao_guarda_o_valor_da_epoca_e_sobrevive_a_apagar_linhas()
    {
        var s = new CalculationSession();
        s.Evaluate("a = 5", NumberBase.Decimal);
        s.PrintVariables();                 // a=5, sep, a(5), sep
        s.Evaluate("a = 9", NumberBase.Decimal);

        s.RemoveLast();                     // volta a valer a=5

        Assert.Equal(4, s.Entries.Count);
        Assert.Equal(new BigInteger(5), s.Entries[2].Value);
        Assert.True(s.Variables.TryGet("a", out var a));
        Assert.Equal(new BigInteger(5), a);
    }

    [Fact]
    public void Impressao_sobrevive_a_gravar_carregar_importar_e_exportar()
    {
        var store = new SessionStore(_dir);
        var original = new CalculationSession();
        original.Evaluate("a = 1234", NumberBase.Hexadecimal);
        original.PrintVariables();
        var path = store.NewSessionPath();
        store.Save(original, path);

        Assert.Contains("\"kind\": \"var\"", File.ReadAllText(path));

        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);
        Assert.Equal(4, loaded.Entries.Count);
        Assert.True(loaded.Entries[2].IsVariablePrint);
        Assert.Equal(new BigInteger(4660), loaded.Entries[2].Value);

        var imported = new CalculationSession();
        var result = HistoryImporter.ImportInto(imported, HistoryImporter.FromDocument(store.Read(path)));
        Assert.Equal(4, result.Imported);
        Assert.True(imported.Entries[2].IsVariablePrint);

        var text = SessionStore.ExportText(loaded);
        Assert.Contains("a = 1234", text);
        Assert.Contains("a = 4660", text);
    }
}
