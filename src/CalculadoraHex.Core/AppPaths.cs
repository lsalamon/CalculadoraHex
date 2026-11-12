namespace CalculadoraHex.Core;

/// <summary>Onde a app guarda sessões e preferências.</summary>
public static class AppPaths
{
    /// <summary>
    /// Se esta variável de ambiente existir, a app usa essa pasta em vez de %LOCALAPPDATA%\CalculadoraHex.
    /// Serve para rodar a app sem tocar nas sessões e preferências do uso normal (testes, versão portátil).
    /// </summary>
    public const string DataDirectoryVariable = "CALCULADORAHEX_DATA";

    public static string DataDirectory
    {
        get
        {
            var custom = Environment.GetEnvironmentVariable(DataDirectoryVariable);
            return !string.IsNullOrWhiteSpace(custom)
                ? Path.GetFullPath(custom)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CalculadoraHex");
        }
    }
}
