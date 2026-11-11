using System.Numerics;
using CalculadoraHex.Core.Evaluation;

namespace CalculadoraHex.Core.Persistence;

/// <summary>Uma linha a importar: expressão (com base), separador ou impressão de variável (nome + valor).</summary>
public sealed record ImportItem(
    string Text,
    NumberBase Base,
    DateTimeOffset? Timestamp = null,
    bool IsSeparator = false,
    BigInteger? PrintedValue = null)
{
    public bool IsVariablePrint => PrintedValue.HasValue;

    public static ImportItem Separator(DateTimeOffset? timestamp = null) =>
        new(string.Empty, NumberBase.Decimal, timestamp, IsSeparator: true);

    public static ImportItem VariablePrint(string name, BigInteger value, DateTimeOffset? timestamp = null) =>
        new(name, NumberBase.Decimal, timestamp, PrintedValue: value);
}

/// <param name="Imported">Linhas acrescentadas à sessão (cálculos, com ou sem erro, e separadores).</param>
/// <param name="Failed">Dessas, quantas resultaram em erro de cálculo.</param>
public sealed record ImportResult(int Imported, int Failed);

/// <summary>
/// Acrescenta operações de outra origem à sessão atual, sem apagar o que já existe.
/// Aceita o JSON de sessão gravado pela própria app e scripts de texto com uma expressão por linha.
/// </summary>
public static class HistoryImporter
{
    private static readonly string[] CommentPrefixes = { "#", ";", "//" };

    /// <summary>
    /// Lê um script de texto. Cada linha não vazia é uma expressão. Prefixos <c>[hex]</c>, <c>[dec]</c>,
    /// <c>hex:</c> ou <c>dec:</c> fixam a base da linha; sem prefixo vale <paramref name="defaultBase"/>.
    /// Linhas iniciadas por <c>#</c>, <c>;</c> ou <c>//</c> são comentários. Uma linha só de traços
    /// (<c>---</c>, três ou mais) vira separador.
    /// </summary>
    public static IReadOnlyList<ImportItem> ParseScript(string content, NumberBase defaultBase)
    {
        ArgumentNullException.ThrowIfNull(content);

        var items = new List<ImportItem>();
        foreach (var rawLine in content.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;
            if (CommentPrefixes.Any(p => line.StartsWith(p, StringComparison.Ordinal))) continue;

            if (IsSeparatorLine(line))
            {
                items.Add(ImportItem.Separator());
                continue;
            }

            var numberBase = defaultBase;
            if (TryStripBasePrefix(ref line, out var explicitBase))
                numberBase = explicitBase;

            if (line.Length == 0) continue;
            items.Add(new ImportItem(line, numberBase));
        }
        return items;
    }

    /// <summary>Converte um documento de sessão gravado em itens, preservando base e data de cada linha.</summary>
    public static IReadOnlyList<ImportItem> FromDocument(SessionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var items = new List<ImportItem>();
        foreach (var e in document.Entries)
        {
            DateTimeOffset? at = e.Timestamp == default ? null : e.Timestamp;
            if (e.IsSeparator)
            {
                items.Add(ImportItem.Separator(at));
            }
            else if (e.IsVariablePrint)
            {
                if (SessionStore.TryParseValue(e.Value, out var printed) && !string.IsNullOrWhiteSpace(e.Text))
                    items.Add(ImportItem.VariablePrint(e.Text, printed, at));
            }
            else if (!string.IsNullOrWhiteSpace(e.Text))
            {
                var b = string.Equals(e.Base, "hex", StringComparison.OrdinalIgnoreCase) ? NumberBase.Hexadecimal : NumberBase.Decimal;
                items.Add(new ImportItem(e.Text, b, at));
            }
        }
        return items;
    }

    /// <summary>Avalia cada item em ordem dentro da sessão. Variáveis já existentes continuam valendo.</summary>
    public static ImportResult ImportInto(CalculationSession session, IEnumerable<ImportItem> items)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(items);

        var imported = 0;
        var failed = 0;
        foreach (var item in items)
        {
            if (item.IsSeparator)
            {
                if (item.Timestamp is { } sepAt) session.AddSeparator(sepAt);
                else session.AddSeparator();
                imported++;
                continue;
            }

            if (item.IsVariablePrint)
            {
                if (item.Timestamp is { } printAt) session.AddVariablePrint(item.Text, item.PrintedValue!.Value, printAt);
                else session.AddVariablePrint(item.Text, item.PrintedValue!.Value);
                imported++;
                continue;
            }

            var entry = item.Timestamp is { } at
                ? session.Evaluate(item.Text, item.Base, at)
                : session.Evaluate(item.Text, item.Base);
            imported++;
            if (!entry.Succeeded) failed++;
        }
        return new ImportResult(imported, failed);
    }

    private static bool IsSeparatorLine(string line) => line.Length >= 3 && line.All(c => c == '-');

    private static bool TryStripBasePrefix(ref string line, out NumberBase numberBase)
    {
        foreach (var (prefix, b) in Prefixes)
        {
            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                line = line[prefix.Length..].TrimStart();
                numberBase = b;
                return true;
            }
        }
        numberBase = default;
        return false;
    }

    private static readonly (string Prefix, NumberBase Base)[] Prefixes =
    {
        ("[hex]", NumberBase.Hexadecimal),
        ("[dec]", NumberBase.Decimal),
        ("hex:", NumberBase.Hexadecimal),
        ("dec:", NumberBase.Decimal),
    };
}
