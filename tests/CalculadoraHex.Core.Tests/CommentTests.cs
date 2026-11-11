using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public sealed class CommentTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "CalculadoraHex.Tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Theory]
    [InlineData("#soma de valores", NumberBase.Decimal)]
    [InlineData("   # com espaços antes e depois   ", NumberBase.Hexadecimal)]
    [InlineData("#", NumberBase.Decimal)]
    [InlineData("# 1/0 não é avaliado", NumberBase.Decimal)]
    public void Texto_com_cerquilha_no_inicio_vira_comentario_literal(string digitado, NumberBase painel)
    {
        var s = new CalculationSession();

        var entry = s.Evaluate(digitado, painel);

        Assert.True(entry.IsComment);
        Assert.True(entry.IsStatic);
        Assert.False(entry.Succeeded);
        Assert.Null(entry.Error);
        Assert.Equal(digitado.Trim(), entry.RawText);
        Assert.Single(s.Entries);
    }

    [Fact]
    public void Cerquilha_no_meio_continua_sendo_erro_de_sintaxe()
    {
        var s = new CalculationSession();

        var entry = s.Evaluate("1 + # 2", NumberBase.Decimal);

        Assert.False(entry.IsComment);
        Assert.NotNull(entry.Error);
    }

    [Fact]
    public void Comentario_nao_altera_variaveis_e_sobrevive_a_apagar_outra_linha()
    {
        var s = new CalculationSession();
        s.Evaluate("a = 5", NumberBase.Decimal);
        s.Evaluate("#soma de valores", NumberBase.Decimal);
        s.Evaluate("b = a + 1", NumberBase.Decimal);

        s.RemoveLast();

        Assert.Equal(2, s.Entries.Count);
        Assert.True(s.Entries[1].IsComment);
        Assert.Equal("#soma de valores", s.Entries[1].RawText);
        Assert.False(s.Variables.IsDefined("b"));
        Assert.True(s.Variables.TryGet("a", out var a));
        Assert.Equal(new BigInteger(5), a);

        s.RemoveLast(); // o próprio comentário pode ser apagado
        Assert.Single(s.Entries);
    }

    [Fact]
    public void Comentario_sobrevive_a_gravar_carregar_e_exportar()
    {
        var store = new SessionStore(_dir);
        var original = new CalculationSession();
        original.Evaluate("#soma de valores", NumberBase.Hexadecimal);
        original.Evaluate("a = 1+2", NumberBase.Decimal);
        var path = store.NewSessionPath();
        store.Save(original, path);

        Assert.Contains("\"kind\": \"cmt\"", File.ReadAllText(path));

        var loaded = new CalculationSession();
        store.LoadInto(loaded, path);
        Assert.Equal(2, loaded.Entries.Count);
        Assert.True(loaded.Entries[0].IsComment);
        Assert.Equal("#soma de valores", loaded.Entries[0].RawText);
        Assert.Equal(NumberBase.Hexadecimal, loaded.Entries[0].InputBase);

        Assert.Contains("#soma de valores", SessionStore.ExportText(loaded));
    }

    [Fact]
    public void Script_importa_linhas_com_cerquilha_como_comentario_sem_contar_erro()
    {
        const string script = "#primeiro bloco\n; anotação ignorada\n// outra ignorada\na = 2\n#segundo";

        var items = HistoryImporter.ParseScript(script, NumberBase.Decimal);
        Assert.Equal(3, items.Count);

        var s = new CalculationSession();
        var result = HistoryImporter.ImportInto(s, items);

        Assert.Equal(3, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.True(s.Entries[0].IsComment);
        Assert.Equal("#primeiro bloco", s.Entries[0].RawText);
        Assert.True(s.Entries[2].IsComment);
    }

    [Fact]
    public void Sessao_gravada_com_comentario_pode_ser_importada()
    {
        var store = new SessionStore(_dir);
        var original = new CalculationSession();
        original.Evaluate("#nota", NumberBase.Decimal);
        var path = store.NewSessionPath();
        store.Save(original, path);

        var target = new CalculationSession();
        var result = HistoryImporter.ImportInto(target, HistoryImporter.FromDocument(store.Read(path)));

        Assert.Equal(1, result.Imported);
        Assert.Equal(0, result.Failed);
        Assert.True(target.Entries[0].IsComment);
    }
}
