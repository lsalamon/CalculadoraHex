using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.App.ViewModels;

/// <summary>Uma linha do display, já renderizada na base do painel.</summary>
public sealed record DisplayLine(
    Guid EntryId,
    IReadOnlyList<TextSegment> Segments,
    string Result,
    bool IsError,
    bool IsSeparator = false,
    bool IsComment = false,
    bool IsRootResult = false,
    string Title = "",
    string Detail = "",
    string Note = "")
{
    public string Expression => string.Concat(Segments.Select(s => s.Text));

    /// <summary>Verdadeiro nas linhas de cálculo e impressão de variável; falso em separadores, comentários e resultados de raiz.</summary>
    public bool IsCalculation => !IsSeparator && !IsComment && !IsRootResult;

    public bool HasNote => Note.Length > 0;

    /// <summary>Texto literal de um comentário (o '#' inicial é mantido).</summary>
    public string CommentText => IsComment ? Result : string.Empty;

    /// <summary>Falso em linhas só de resultado (impressão de variável): o display não reserva a linha da expressão.</summary>
    public bool HasExpression => Segments.Count > 0;

    public static DisplayLine From(CalculationEntry entry, NumberBase paneBase)
    {
        if (entry.IsSeparator)
            return new DisplayLine(entry.Id, Array.Empty<TextSegment>(), string.Empty, IsError: false, IsSeparator: true);

        if (entry.IsComment)
            return new DisplayLine(entry.Id, Array.Empty<TextSegment>(), entry.RawText, IsError: false, IsComment: true);

        if (entry.IsRootResult && entry.Root is { } root)
        {
            var text = RootFormatter.Format(entry.RawText, root, paneBase);
            return new DisplayLine(entry.Id, Array.Empty<TextSegment>(), string.Empty, IsError: false,
                IsRootResult: true, Title: text.Title, Detail: text.Detail, Note: text.Note ?? string.Empty);
        }

        if (entry.IsVariablePrint && entry.Value is { } printed)
        {
            var text = $"{entry.RawText} = {NumberFormatter.Format(printed, paneBase)}";
            return new DisplayLine(entry.Id, Array.Empty<TextSegment>(), text, IsError: false);
        }

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
