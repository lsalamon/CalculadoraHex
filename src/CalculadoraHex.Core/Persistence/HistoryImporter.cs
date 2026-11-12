using System.Numerics;
using CalculadoraHex.Core.Evaluation;

namespace CalculadoraHex.Core.Persistence;

/// <summary>Uma linha a importar: expressão (com base), separador ou impressão de variável (nome + valor).</summary>
public sealed record ImportItem(
    string Text,
    NumberBase Base,
    DateTimeOffset? Timestamp = null,
    bool IsSeparator = false,
    BigInteger? PrintedValue = null,
    bool BaseIsExplicit = false)
{
    public bool IsVariablePrint => PrintedValue.HasValue;

    /// <summary>
    /// Verdadeiro quando a linha é uma expressão cuja base só a escolha do usuário define
    /// (sem prefixo de base). Comentários, separadores e impressões de variável não dependem de base.
    /// </summary>
    public bool NeedsBase => !IsSeparator && !IsVariablePrint && !BaseIsExplicit && !Text.StartsWith('#');

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
    /// <summary>Linhas iniciadas por estes prefixos são anotações do arquivo e não entram no histórico.</summary>
    private static readonly string[] IgnoredPrefixes = { ";", "//" };

    /// <summary>
    /// Lê um script de texto. Cada linha não vazia é uma expressão. Prefixos <c>[hex]</c>, <c>[dec]</c>,
    /// <c>hex:</c> ou <c>dec:</c> fixam a base da linha; sem prefixo vale <paramref name="defaultBase"/>.
    /// Linhas iniciadas por <c>#</c> viram comentários no histórico (como ao digitar na calculadora);
    /// as iniciadas por <c>;</c> ou <c>//</c> são ignoradas. Uma linha só de traços
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
            if (IgnoredPrefixes.Any(p => line.StartsWith(p, StringComparison.Ordinal))) continue;

            // '#' vai como está (inclusive um ';' no texto): a sessão o reconhece como comentário ao avaliar a linha.
            if (line.StartsWith('#'))
            {
                items.Add(new ImportItem(line, defaultBase));
                continue;
            }

            // Anotação no fim da linha: expressões não têm ';', então o que vem depois é só comentário.
            var semicolon = line.IndexOf(';');
            if (semicolon >= 0)
            {
                line = line[..semicolon].TrimEnd();
                if (line.Length == 0) continue;
            }

            if (IsSeparatorLine(line))
            {
                items.Add(ImportItem.Separator());
                continue;
            }

            if (TryParseVariablePrint(line, out var printName, out var printValue))
            {
                items.Add(ImportItem.VariablePrint(printName, printValue));
                continue;
            }

            var numberBase = defaultBase;
            var explicitBase = TryStripBasePrefix(ref line, out var prefixBase);
            if (explicitBase) numberBase = prefixBase;

            if (line.Length == 0) continue;
            items.Add(new ImportItem(line, numberBase, BaseIsExplicit: explicitBase));
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
            else if (e.IsRoot)
            {
                // Resultado derivado de uma variável: não é recriado na importação.
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
            if (!entry.IsStatic && !entry.Succeeded) failed++;
        }
        return new ImportResult(imported, failed);
    }

    private static bool IsSeparatorLine(string line) => line.Length >= 3 && line.All(c => c == '-');

    /// <summary>Reconhece <c>[var] nome = valor</c> (valor decimal inteiro), o formato que a exportação usa para variáveis impressas.</summary>
    private static bool TryParseVariablePrint(string line, out string name, out BigInteger value)
    {
        name = string.Empty;
        value = default;

        const string prefix = "[var]";
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;

        var rest = line[prefix.Length..];
        var equals = rest.IndexOf('=');
        if (equals < 0) return false;

        var candidate = rest[..equals].Trim();
        if (!Parsing.Parser.IsIdentifier(candidate)) return false;

        if (!BigInteger.TryParse(rest[(equals + 1)..].Trim(), System.Globalization.NumberStyles.AllowLeadingSign,
                System.Globalization.CultureInfo.InvariantCulture, out value))
            return false;

        name = candidate;
        return true;
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
