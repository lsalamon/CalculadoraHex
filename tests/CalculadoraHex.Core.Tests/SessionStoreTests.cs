using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class SessionStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));
    private readonly SessionStore _store;

    public SessionStoreTests() => _store = new SessionStore(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Salvar_e_carregar_reconstroi_historico_e_variaveis()
    {
        var original = new CalculationSession();
        original.Evaluate("a=1234", NumberBase.Hexadecimal);
        original.Evaluate("a=a*4321", NumberBase.Hexadecimal);
        original.Evaluate("b = a / 0", NumberBase.Decimal);
        var path = _store.NewSessionPath();

        _store.Save(original, path);

        var loaded = new CalculationSession();
        var added = 0;
        loaded.EntryAdded += (_, _) => added++;
        _store.LoadInto(loaded, path);

        Assert.Equal(3, added);
        Assert.Equal(3, loaded.Entries.Count);
        Assert.Equal(new BigInteger(80082100), loaded.Entries[1].Value);
        Assert.False(loaded.Entries[2].Succeeded);
        Assert.Equal(original.Entries[0].Timestamp, loaded.Entries[0].Timestamp);
        Assert.True(loaded.Variables.TryGet("a", out var a));
        Assert.Equal(new BigInteger(80082100), a);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Variavel_removida_antes_de_salvar_continua_removida_ao_carregar()
    {
        var original = new CalculationSession();
        original.Evaluate("x = 5", NumberBase.Decimal);
        original.Evaluate("y = 6", NumberBase.Decimal);
        original.Variables.Remove("x");
        var path = _store.NewSessionPath();
        _store.Save(original, path);

        var loaded = new CalculationSession();
        _store.LoadInto(loaded, path);

        Assert.False(loaded.Variables.IsDefined("x"));
        Assert.True(loaded.Variables.IsDefined("y"));
        Assert.Equal(2, loaded.Entries.Count);
    }

    [Fact]
    public void Lista_sessoes_mais_recentes_primeiro_e_ignora_arquivos_invalidos()
    {
        var s1 = new CalculationSession();
        s1.Evaluate("1+1", NumberBase.Decimal);
        var p1 = Path.Combine(_dir, "antiga.json");
        _store.Save(s1, p1);

        var s2 = new CalculationSession();
        s2.Evaluate("2+2", NumberBase.Decimal);
        s2.Evaluate("3+3", NumberBase.Decimal);
        var p2 = Path.Combine(_dir, "nova.json");
        Thread.Sleep(20);
        _store.Save(s2, p2);

        File.WriteAllText(Path.Combine(_dir, "lixo.json"), "{ isso não é json");

        var list = _store.ListSessions();

        Assert.Equal(2, list.Count);
        Assert.Equal("nova", list[0].Name);
        Assert.Equal(2, list[0].EntryCount);
        Assert.Equal("antiga", list[1].Name);
    }

    [Fact]
    public void Ponteiro_da_ultima_sessao_so_vale_se_o_arquivo_existir()
    {
        Assert.Null(_store.GetLastSessionPath());

        var path = _store.NewSessionPath();
        _store.Save(new CalculationSession(), path);
        _store.SetLastSessionPath(path);
        Assert.Equal(path, _store.GetLastSessionPath());

        File.Delete(path);
        Assert.Null(_store.GetLastSessionPath());
    }

    [Fact]
    public void Caminhos_novos_nao_colidem()
    {
        var at = new DateTimeOffset(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);
        var p1 = _store.NewSessionPath(at);
        _store.Save(new CalculationSession(), p1);
        var p2 = _store.NewSessionPath(at);

        Assert.NotEqual(p1, p2);
        Assert.EndsWith("2026-11-09_100000.json", p1);
        Assert.EndsWith("2026-11-09_100000_2.json", p2);
    }

    [Fact]
    public void Exporta_texto_com_as_duas_bases()
    {
        var s = new CalculationSession();
        s.Evaluate("a=1234", NumberBase.Hexadecimal);
        s.Evaluate("a=a*4321", NumberBase.Hexadecimal);

        var text = SessionStore.ExportText(s);

        Assert.Contains("a = a * 4321", text);
        Assert.Contains("a = a * 17185", text);
        Assert.Contains("a = 4C5F4B4", text);
        Assert.Contains("a = 80082100", text);
        Assert.Contains("a = 4C5F4B4h = 80082100", text);
    }
}
