using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Persistence;

public sealed record SessionInfo(string Path, string Name, DateTimeOffset UpdatedAt, int EntryCount);

/// <summary>
/// Grava e carrega sessões em JSON. A gravação é atômica (arquivo temporário + rename).
/// Ao carregar, as linhas são reavaliadas em ordem; o snapshot de variáveis gravado prevalece no fim.
/// </summary>
public sealed class SessionStore
{
    public const string FileExtension = ".json";

    /// <summary>Como um separador aparece no texto exportado (e é reconhecido ao importar).</summary>
    public static readonly string SeparatorLine = new('-', 40);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SessionStore(string directory)
    {
        Directory = directory ?? throw new ArgumentNullException(nameof(directory));
    }

    /// <summary>%LOCALAPPDATA%\CalculadoraHex\sessoes</summary>
    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalculadoraHex", "sessoes");

    public string Directory { get; }

    private string LastSessionPointerPath => Path.Combine(Directory, "ultima.txt");

    /// <summary>Caminho novo, com carimbo de data/hora, dentro do diretório padrão.</summary>
    public string NewSessionPath(DateTimeOffset? at = null)
    {
        var stamp = (at ?? DateTimeOffset.Now).ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
        var path = Path.Combine(Directory, stamp + FileExtension);
        var n = 1;
        while (File.Exists(path))
            path = Path.Combine(Directory, $"{stamp}_{++n}{FileExtension}");
        return path;
    }

    public void Save(CalculationSession session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        var document = ToDocument(session);
        var json = JsonSerializer.Serialize(document, JsonOptions);

        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, json, Encoding.UTF8);
        File.Move(tmp, path, overwrite: true);
    }

    /// <summary>Lê o arquivo e reconstrói a sessão dentro de <paramref name="session"/>, zerando o que havia.</summary>
    public void LoadInto(CalculationSession session, string path)
    {
        ArgumentNullException.ThrowIfNull(session);
        var document = Read(path);

        session.Clear(document.CreatedAt == default ? DateTimeOffset.Now : document.CreatedAt);

        foreach (var entry in document.Entries)
        {
            if (entry.IsSeparator)
                session.AddSeparator(entry.Timestamp);
            else if (entry.IsVariablePrint)
            {
                if (TryParseValue(entry.Value, out var printed) && !string.IsNullOrWhiteSpace(entry.Text))
                    session.AddVariablePrint(entry.Text, printed, entry.Timestamp);
            }
            else
                session.Evaluate(entry.Text, ParseBase(entry.Base), entry.Timestamp);
        }

        var variables = new Dictionary<string, BigInteger>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in document.Variables)
        {
            if (TryParseValue(kv.Value, out var value))
                variables[kv.Key] = value;
        }
        session.Variables.ReplaceAll(variables);
    }

    internal static bool TryParseValue(string? text, out BigInteger value) =>
        BigInteger.TryParse(text ?? string.Empty, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);

    public SessionDocument Read(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var document = JsonSerializer.Deserialize<SessionDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Arquivo de sessão vazio.");
        if (document.Version > SessionDocument.CurrentVersion)
            throw new InvalidDataException($"Sessão gravada por versão mais nova (v{document.Version}).");
        return document;
    }

    public IReadOnlyList<SessionInfo> ListSessions()
    {
        if (!System.IO.Directory.Exists(Directory)) return Array.Empty<SessionInfo>();

        var result = new List<SessionInfo>();
        foreach (var file in System.IO.Directory.EnumerateFiles(Directory, "*" + FileExtension))
        {
            try
            {
                var doc = Read(file);
                result.Add(new SessionInfo(file, Path.GetFileNameWithoutExtension(file), doc.UpdatedAt, doc.Entries.Count));
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException)
            {
                // Arquivo corrompido ou de outro formato: ignorado na listagem.
            }
        }
        return result.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public string? GetLastSessionPath()
    {
        try
        {
            if (!File.Exists(LastSessionPointerPath)) return null;
            var path = File.ReadAllText(LastSessionPointerPath, Encoding.UTF8).Trim();
            return path.Length > 0 && File.Exists(path) ? path : null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void SetLastSessionPath(string? path)
    {
        System.IO.Directory.CreateDirectory(Directory);
        if (path is null)
        {
            File.Delete(LastSessionPointerPath);
            return;
        }
        File.WriteAllText(LastSessionPointerPath, path, Encoding.UTF8);
    }

    /// <summary>Exporta o histórico em texto legível, com cada linha nas duas bases.</summary>
    public static string ExportText(CalculationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var sb = new StringBuilder();
        sb.AppendLine("CalculadoraHex - histórico");
        sb.AppendLine($"Sessão criada em {session.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine($"{"Hexadecimal",-40}  {"Decimal",-40}");
        sb.AppendLine(new string('-', 82));

        foreach (var entry in session.Entries)
        {
            if (entry.IsSeparator)
            {
                sb.AppendLine(SeparatorLine);
                sb.AppendLine();
                continue;
            }

            if (entry.IsVariablePrint)
            {
                sb.AppendLine($"{ResultText(entry, NumberBase.Hexadecimal),40}  {ResultText(entry, NumberBase.Decimal),40}");
                continue;
            }

            if (entry.Succeeded && entry.Statement is not null)
            {
                sb.AppendLine($"{ExpressionPrinter.Print(entry.Statement, NumberBase.Hexadecimal),-40}  {ExpressionPrinter.Print(entry.Statement, NumberBase.Decimal),-40}");
                var hex = ResultText(entry, NumberBase.Hexadecimal);
                var dec = ResultText(entry, NumberBase.Decimal);
                sb.AppendLine($"{hex,40}  {dec,40}");
            }
            else
            {
                var tag = entry.InputBase == NumberBase.Hexadecimal ? "hex" : "dec";
                sb.AppendLine($"[{tag}] {entry.RawText}");
                sb.AppendLine($"{"ERRO: " + entry.Error,82}");
            }
            sb.AppendLine();
        }

        var vars = session.Variables.Snapshot();
        if (vars.Count > 0)
        {
            sb.AppendLine("Variáveis:");
            foreach (var kv in vars.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"  {kv.Key} = {NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal)}h = {NumberFormatter.Format(kv.Value, NumberBase.Decimal)}");
        }

        return sb.ToString();
    }

    private static string ResultText(CalculationEntry entry, NumberBase numberBase)
    {
        var value = NumberFormatter.Format(entry.Value!.Value, numberBase);
        return entry.AssignedVariable is null ? value : $"{entry.AssignedVariable} = {value}";
    }

    private static SessionDocument ToDocument(CalculationSession session)
    {
        var doc = new SessionDocument
        {
            CreatedAt = session.CreatedAt,
            UpdatedAt = DateTimeOffset.Now,
        };

        foreach (var e in session.Entries)
        {
            doc.Entries.Add(new EntryDocument
            {
                Timestamp = e.Timestamp,
                Kind = e.Kind switch
                {
                    EntryKind.Separator => EntryDocument.SeparatorKind,
                    EntryKind.VariablePrint => EntryDocument.VariablePrintKind,
                    _ => null,
                },
                Base = e.InputBase == NumberBase.Hexadecimal ? "hex" : "dec",
                Text = e.RawText,
                Value = e.Value?.ToString(CultureInfo.InvariantCulture),
                Error = e.Error,
            });
        }

        foreach (var kv in session.Variables.Snapshot())
            doc.Variables[kv.Key] = kv.Value.ToString(CultureInfo.InvariantCulture);

        return doc;
    }

    private static NumberBase ParseBase(string text) =>
        string.Equals(text, "hex", StringComparison.OrdinalIgnoreCase) ? NumberBase.Hexadecimal : NumberBase.Decimal;
}
