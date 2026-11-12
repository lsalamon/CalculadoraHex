using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class RootEntryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static CalculationSession SessionWith1234()
    {
        var s = new CalculationSession();
        s.Evaluate("a=4D2", NumberBase.Hexadecimal); // 1234
        return s;
    }

    [Fact]
    public void Usa_o_valor_atual_da_variavel_e_entra_no_historico()
    {
        var s = SessionWith1234();

        var entry = s.AddRootResult("a", 3);

        Assert.True(entry.IsRootResult);
        Assert.True(entry.IsStatic);
        Assert.Equal(2, s.Entries.Count);
        Assert.Equal(new BigInteger(1234), entry.Value);
        Assert.Equal(3, entry.RootExponent);
        Assert.Equal(new BigInteger(10), entry.Root!.Root);
        Assert.Equal(new BigInteger(234), entry.Root.Difference);
    }

    [Fact]
    public void Variavel_inexistente_nao_acrescenta_nada()
    {
        var s = SessionWith1234();

        var ex = Assert.Throws<CalculationException>(() => s.AddRootResult("zzz", 2));

        Assert.Contains("não definida", ex.Message);
        Assert.Single(s.Entries);
    }

    [Fact]
    public void Variavel_negativa_ou_n_invalido_nao_acrescentam_nada()
    {
        var s = new CalculationSession();
        s.Evaluate("n = 0 - 5", NumberBase.Decimal);
        s.Evaluate("p = 9", NumberBase.Decimal);

        Assert.Throws<CalculationException>(() => s.AddRootResult("n", 2));
        Assert.Throws<CalculationException>(() => s.AddRootResult("p", 0));
        Assert.Equal(2, s.Entries.Count);
    }

    [Fact]
    public void Resultado_guarda_o_valor_da_epoca_e_sobrevive_a_apagar_outra_linha()
    {
        var s = SessionWith1234();
        s.AddRootResult("a", 3);
        s.Evaluate("a = 1", NumberBase.Decimal);

        s.RemoveLast();

        Assert.Equal(2, s.Entries.Count);
        Assert.True(s.Entries[1].IsRootResult);
        Assert.Equal(new BigInteger(1234), s.Entries[1].Value);
        Assert.True(s.Variables.TryGet("a", out var a));
        Assert.Equal(new BigInteger(1234), a);

        s.RemoveLast(); // a própria linha de resultado pode ser apagada
        Assert.Single(s.Entries);
    }

    [Fact]
    public void Sobrevive_a_gravar_e_carregar_recalculando_o_resultado()
    {
        var store = new SessionStore(_dir);
        var original = SessionWith1234();
        original.AddRootResult("a", 3);
        var path = store.NewSessionPath();
        store.Save(original, path);

        var json = File.ReadAllText(path);
        Assert.Contains("\"kind\": \"root\"", json);
        Assert.Contains("\"exponent\": 3", json);

        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);

        Assert.Equal(2, loaded.Entries.Count);
        var entry = loaded.Entries[1];
        Assert.True(entry.IsRootResult);
        Assert.Equal("a", entry.RawText);
        Assert.Equal(new BigInteger(10), entry.Root!.Root);
        Assert.Equal(new BigInteger(97), entry.Root.NextExcess);
    }

    [Fact]
    public void Arquivo_com_resultado_invalido_e_descartado_sem_quebrar_o_carregamento()
    {
        var store = new SessionStore(_dir);
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "editado.json");
        File.WriteAllText(path,
            "{\"version\":1,\"entries\":[{\"kind\":\"root\",\"text\":\"a\",\"value\":\"-5\",\"exponent\":2,\"base\":\"dec\"}," +
            "{\"kind\":\"root\",\"text\":\"b\",\"value\":\"9\",\"exponent\":0,\"base\":\"dec\"}," +
            "{\"base\":\"dec\",\"text\":\"1+1\"}],\"variables\":{}}");

        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);

        Assert.Single(loaded.Entries);
        Assert.Equal("1+1", loaded.Entries[0].RawText);
    }

    [Fact]
    public void Exportacao_traz_a_anotacao_nas_duas_bases_e_nao_e_recriada_ao_importar()
    {
        var s = SessionWith1234();
        s.AddRootResult("a", 3);

        var text = SessionStore.ExportText(s);

        Assert.Contains("; raiz 3 de a = 1234 (hex 4D2)", text);
        Assert.Contains(";   dec: x = 10    x^3 = 1000    diferença 234", text);
        Assert.Contains(";   dec: x = 11 daria 1331, que passa 97", text);
        Assert.Contains(";   hex: x = A    x^3 = 3E8    diferença EA", text);
        Assert.Contains(";   hex: x = B daria 533, que passa 61", text);

        var items = HistoryImporter.ParseScript(text, NumberBase.Decimal);
        Assert.DoesNotContain(items, i => i.Text.Contains("raiz"));

        var imported = new CalculationSession();
        var result = HistoryImporter.ImportInto(imported, items);
        Assert.Equal(0, result.Failed);
        Assert.Single(imported.Entries); // só a atribuição a=4D2
    }

    [Fact]
    public void Importar_sessao_json_nao_recria_o_resultado_da_raiz()
    {
        var store = new SessionStore(_dir);
        var original = SessionWith1234();
        original.AddRootResult("a", 3);
        var path = store.NewSessionPath();
        store.Save(original, path);

        var target = new CalculationSession();
        var result = HistoryImporter.ImportInto(target, HistoryImporter.FromDocument(store.Read(path)));

        Assert.Equal(1, result.Imported);
        Assert.False(target.Entries[0].IsRootResult);
    }
}
