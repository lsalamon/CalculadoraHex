using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class EditTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Apagar_ultima_linha_reverte_a_variavel_atribuida()
    {
        var s = new CalculationSession();
        s.Evaluate("a = 5", NumberBase.Decimal);
        s.Evaluate("a = a * 10", NumberBase.Decimal);
        var replaced = 0;
        s.HistoryReplaced += (_, _) => replaced++;

        Assert.True(s.RemoveLast());

        Assert.Single(s.Entries);
        Assert.True(s.Variables.TryGet("a", out var a));
        Assert.Equal(new BigInteger(5), a);
        Assert.Equal(1, replaced);
    }

    [Fact]
    public void Apagar_a_unica_atribuicao_remove_a_variavel()
    {
        var s = new CalculationSession();
        s.Evaluate("b = 7", NumberBase.Decimal);

        s.RemoveLast();

        Assert.Empty(s.Entries);
        Assert.False(s.Variables.IsDefined("b"));
        Assert.False(s.RemoveLast());
    }

    [Fact]
    public void Separador_entra_no_historico_e_pode_ser_apagado()
    {
        var s = new CalculationSession();
        var added = new List<CalculationEntry>();
        s.EntryAdded += (_, e) => added.Add(e);

        s.Evaluate("1+1", NumberBase.Decimal);
        var sep = s.AddSeparator();

        Assert.True(sep.IsSeparator);
        Assert.False(sep.Succeeded);
        Assert.Equal(2, s.Entries.Count);
        Assert.Same(sep, added[1]);

        s.RemoveLast();
        Assert.Single(s.Entries);
        Assert.False(s.Entries[0].IsSeparator);
    }

    [Fact]
    public void Separador_sobrevive_a_gravar_carregar_e_exportar()
    {
        var store = new SessionStore(_dir);
        var original = new CalculationSession();
        original.Evaluate("x = 1", NumberBase.Decimal);
        original.AddSeparator();
        original.Evaluate("y = x + 1", NumberBase.Decimal);
        var path = store.NewSessionPath();
        store.Save(original, path);

        var json = File.ReadAllText(path);
        Assert.Contains("\"kind\": \"sep\"", json);

        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);
        Assert.Equal(3, loaded.Entries.Count);
        Assert.True(loaded.Entries[1].IsSeparator);
        Assert.Equal(new BigInteger(2), loaded.Entries[2].Value);

        var text = SessionStore.ExportText(loaded);
        Assert.Contains(SessionStore.SeparatorLine, text);
    }

    [Fact]
    public void Script_importado_reconhece_linha_de_tracos_como_separador()
    {
        var items = HistoryImporter.ParseScript("1+1\n---\n----------\n--\n2+2", NumberBase.Decimal);

        Assert.Equal(5, items.Count);
        Assert.False(items[0].IsSeparator);
        Assert.True(items[1].IsSeparator);
        Assert.True(items[2].IsSeparator);
        Assert.False(items[3].IsSeparator); // "--" é curto demais: vira expressão (e dará erro)
        Assert.Equal("2+2", items[4].Text);

        var s = new CalculationSession();
        var result = HistoryImporter.ImportInto(s, items);
        Assert.Equal(5, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.True(s.Entries[1].IsSeparator);
    }
}
