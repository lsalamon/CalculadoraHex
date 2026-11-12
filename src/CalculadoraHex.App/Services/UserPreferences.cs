using System.IO;
using System.Text.Json;

namespace CalculadoraHex.App.Services;

/// <summary>Preferências do usuário (não confundir com a sessão de cálculo), gravadas em %LOCALAPPDATA%\CalculadoraHex.</summary>
public sealed class UserPreferences
{
    public static readonly string Directory = CalculadoraHex.Core.AppPaths.DataDirectory;

    private static readonly string FilePath = Path.Combine(Directory, "preferencias.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public int Version { get; set; } = 1;

    public bool HexPaneVisible { get; set; } = true;

    public bool DecimalPaneVisible { get; set; } = true;

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public static UserPreferences Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new UserPreferences();
            var prefs = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(FilePath), JsonOptions);
            return prefs ?? new UserPreferences();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Preferências corrompidas ou inacessíveis: segue com os padrões.
            return new UserPreferences();
        }
    }

    public void Save()
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            var tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOptions));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Preferência é conveniência; não derruba a app se o disco falhar.
        }
    }
}
