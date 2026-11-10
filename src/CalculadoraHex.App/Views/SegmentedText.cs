using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.App.Views;

/// <summary>
/// Propriedade anexada que preenche um <see cref="TextBlock"/> com os segmentos de uma expressão,
/// colorindo variáveis e sublinhando tokens ambíguos (número × variável no painel hexa).
/// </summary>
public static class SegmentedText
{
    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.RegisterAttached(
        "Segments", typeof(IReadOnlyList<TextSegment>), typeof(SegmentedText),
        new PropertyMetadata(null, OnSegmentsChanged));

    public static void SetSegments(DependencyObject element, IReadOnlyList<TextSegment>? value) =>
        element.SetValue(SegmentsProperty, value);

    public static IReadOnlyList<TextSegment>? GetSegments(DependencyObject element) =>
        (IReadOnlyList<TextSegment>?)element.GetValue(SegmentsProperty);

    private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;

        textBlock.Inlines.Clear();
        if (e.NewValue is not IReadOnlyList<TextSegment> segments) return;

        var variableBrush = textBlock.TryFindResource("DisplayForegroundBrush") as Brush;

        foreach (var segment in segments)
        {
            var run = new Run(segment.Text);

            if (segment.Kind == SegmentKind.Variable && variableBrush is not null)
                run.Foreground = variableBrush;

            if (segment.Ambiguous)
            {
                run.TextDecorations = TextDecorations.Underline;
                run.ToolTip = segment.Kind == SegmentKind.Variable
                    ? $"'{segment.Text}' foi lido como variável (há uma variável com esse nome). Use 0x{segment.Text} para forçar número."
                    : $"'{segment.Text}' foi lido como número hexa (não há variável com esse nome). Use ${segment.Text} para forçar variável.";
            }

            textBlock.Inlines.Add(run);
        }
    }
}
