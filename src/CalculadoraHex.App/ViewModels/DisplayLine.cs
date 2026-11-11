using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.App.ViewModels;

/// <summary>Uma linha do display, já renderizada na base do painel.</summary>
public sealed record DisplayLine(Guid EntryId, IReadOnlyList<TextSegment> Segments, string Result, bool IsError, bool IsSeparator = false)
{
    public string Expression => string.Concat(Segments.Select(s => s.Text));

    public static DisplayLine From(CalculationEntry entry, NumberBase paneBase)
    {
        if (entry.IsSeparator)
            return new DisplayLine(entry.Id, Array.Empty<TextSegment>(), string.Empty, IsError: false, IsSeparator: true);

        if (entry.Succeeded && entry.Statement is not null)
        {
            var segments = ExpressionPrinter.PrintSegments(entry.Statement, paneBase);
            // A ambiguidade número × variável só existe no painel hexa; no decimal não há o que realçar.
            if (paneBase != NumberBase.Hexadecimal)
                segments = segments.Select(s => s with { Ambiguous = false }).ToList();
            var value = NumberFormatter.Format(entry.Value!.Value, paneBase);
            var result = entry.AssignedVariable is null ? value : $"{entry.AssignedVariable} = {value}";
            return new DisplayLine(entry.Id, segments, result, IsError: false);
        }

        // Linha com erro: não há árvore para reescrever; mostramos o texto original e a base em que foi digitado.
        var origin = entry.InputBase == paneBase ? string.Empty : $"[{BaseTag(entry.InputBase)}] ";
        var raw = new[] { new TextSegment(origin + entry.RawText, SegmentKind.Punctuation) };
        return new DisplayLine(entry.Id, raw, "ERRO: " + (entry.Error ?? "desconhecido"), IsError: true);
    }

    private static string BaseTag(NumberBase numberBase) => numberBase == NumberBase.Hexadecimal ? "hex" : "dec";
}
