using CalculadoraHex.Core.Evaluation;

namespace CalculadoraHex.Core.Persistence;

/// <summary>Uma operação a importar: texto, base em que deve ser lida e, se conhecido, o momento original.</summary>
public sealed record ImportItem(string Text, NumberBase Base, DateTimeOffset? Timestamp = null);

/// <param name="Imported">Linhas acrescentadas à sessão (com ou sem erro de cálculo).</param>
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
    /// Linhas iniciadas por <c>#</c>, <c>;</c> ou <c>//</c> são comentários.
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
        return document.Entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Text))
            .Select(e => new ImportItem(
                e.Text,
                string.Equals(e.Base, "hex", StringComparison.OrdinalIgnoreCase) ? NumberBase.Hexadecimal : NumberBase.Decimal,
                e.Timestamp == default ? null : e.Timestamp))
            .ToList();
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
            var entry = item.Timestamp is { } at
                ? session.Evaluate(item.Text, item.Base, at)
                : session.Evaluate(item.Text, item.Base);
            imported++;
            if (!entry.Succeeded) failed++;
        }
        return new ImportResult(imported, failed);
    }

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
