using System.Numerics;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.Core.Tests;

public class HistoryImporterTests
{
    [Fact]
    public void Script_usa_base_padrao_e_respeita_prefixos_e_comentarios()
    {
        const string script = """
            # comentário
            a = 1234
            [dec] b = 10
            hex: c = FF
            DEC: d = 7

            ; outro comentário
            // e mais um
               e = a + 1
            """;

        var items = HistoryImporter.ParseScript(script, NumberBase.Hexadecimal);

        Assert.Collection(items,
            i => { Assert.Equal("a = 1234", i.Text); Assert.Equal(NumberBase.Hexadecimal, i.Base); },
            i => { Assert.Equal("b = 10", i.Text); Assert.Equal(NumberBase.Decimal, i.Base); },
            i => { Assert.Equal("c = FF", i.Text); Assert.Equal(NumberBase.Hexadecimal, i.Base); },
            i => { Assert.Equal("d = 7", i.Text); Assert.Equal(NumberBase.Decimal, i.Base); },
            i => { Assert.Equal("e = a + 1", i.Text); Assert.Equal(NumberBase.Hexadecimal, i.Base); });
    }

    [Fact]
    public void Script_com_quebras_de_linha_windows()
    {
        var items = HistoryImporter.ParseScript("1+1\r\n2+2\r\n", NumberBase.Decimal);
        Assert.Equal(2, items.Count);
        Assert.Equal("2+2", items[1].Text);
    }

    [Fact]
    public void Importar_acrescenta_sem_apagar_a_sessao_atual()
    {
        var session = new CalculationSession();
        session.Evaluate("x = 100", NumberBase.Decimal);

        var result = HistoryImporter.ImportInto(session, new[]
        {
            new ImportItem("y = x + 1", NumberBase.Decimal),
            new ImportItem("z = 1/0", NumberBase.Decimal),
            new ImportItem("w = FF", NumberBase.Hexadecimal),
        });

        Assert.Equal(3, result.Imported);
        Assert.Equal(1, result.Failed);
        Assert.Equal(4, session.Entries.Count);
        Assert.True(session.Variables.TryGet("y", out var y));
        Assert.Equal(new BigInteger(101), y);
        Assert.False(session.Variables.IsDefined("z"));
        Assert.True(session.Variables.TryGet("w", out var w));
        Assert.Equal(new BigInteger(255), w);
    }

    [Fact]
    public void Importar_de_documento_preserva_base_e_data()
    {
        var at = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var doc = new SessionDocument
        {
            Entries =
            {
                new EntryDocument { Base = "hex", Text = "a = 10", Timestamp = at },
                new EntryDocument { Base = "dec", Text = "   " },
                new EntryDocument { Base = "dec", Text = "b = a * 2" },
            },
        };

        var items = HistoryImporter.FromDocument(doc);
        Assert.Equal(2, items.Count);
        Assert.Equal(NumberBase.Hexadecimal, items[0].Base);
        Assert.Equal(at, items[0].Timestamp);
        Assert.Null(items[1].Timestamp);

        var session = new CalculationSession();
        HistoryImporter.ImportInto(session, items);
        Assert.Equal(at, session.Entries[0].Timestamp);
        Assert.Equal(new BigInteger(32), session.Entries[1].Value);
    }
}
