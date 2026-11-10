using System.IO;
using System.Windows;
using CalculadoraHex.App.Services;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CalculadoraHex.Core.Persistence;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace CalculadoraHex.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private const string SessionFilter = "Sessão CalculadoraHex (*.json)|*.json|Todos os arquivos (*.*)|*.*";
    private const string TextFilter = "Texto (*.txt)|*.txt|Todos os arquivos (*.*)|*.*";

    public MainViewModel()
    {
        Session = new CalculationSession();
        HexPane = new PaneViewModel(Session, NumberBase.Hexadecimal);
        DecimalPane = new PaneViewModel(Session, NumberBase.Decimal);
        Sessions = new SessionManager(Session, new SessionStore(SessionStore.DefaultDirectory));

        Session.EntryAdded += (_, entry) =>
        {
            HexPane.Append(entry);
            DecimalPane.Append(entry);
        };
        Session.Reset += (_, _) =>
        {
            HexPane.ClearLines();
            DecimalPane.ClearLines();
        };
        Session.Variables.Changed += (_, _) => UpdateVariablesSummary();

        Sessions.CurrentPathChanged += (_, _) => UpdateSessionName();
        Sessions.SaveFailed += (_, ex) => StatusMessage = "Falha ao gravar a sessão: " + ex.Message;

        UpdateVariablesSummary();
        UpdateSessionName();
    }

    public CalculationSession Session { get; }

    public SessionManager Sessions { get; }

    public PaneViewModel HexPane { get; }

    public PaneViewModel DecimalPane { get; }

    [ObservableProperty]
    private string _variablesSummary = string.Empty;

    [ObservableProperty]
    private string _sessionName = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Chamado pela janela depois de carregada, para que os painéis já existam ao repovoar.</summary>
    public void RestoreLastSession()
    {
        if (Sessions.TryRestoreLast())
            StatusMessage = "Última sessão reaberta.";
    }

    public void Shutdown() => Sessions.FlushSave();

    [RelayCommand]
    private void NewSession()
    {
        if (Session.Entries.Count > 0)
        {
            var answer = MessageBox.Show(
                "Iniciar uma nova sessão? A atual já está gravada e pode ser reaberta em Arquivo > Abrir sessão.",
                "Nova sessão", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;
        }

        Sessions.StartNew();
        StatusMessage = "Nova sessão. O arquivo será criado na primeira operação.";
    }

    [RelayCommand]
    private void OpenSession()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Abrir sessão",
            Filter = SessionFilter,
            InitialDirectory = EnsureDirectory(Sessions.SessionsDirectory),
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Sessions.Open(dialog.FileName);
            StatusMessage = $"Sessão aberta: {Session.Entries.Count} operações.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show("Não foi possível abrir a sessão.\n\n" + ex.Message, "Abrir sessão", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveSessionAs()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Salvar sessão como",
            Filter = SessionFilter,
            DefaultExt = SessionStore.FileExtension,
            AddExtension = true,
            InitialDirectory = EnsureDirectory(Sessions.SessionsDirectory),
            FileName = Sessions.CurrentPath is null ? "sessao" : Path.GetFileNameWithoutExtension(Sessions.CurrentPath),
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Sessions.SaveAs(dialog.FileName);
            StatusMessage = "Sessão gravada.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("Não foi possível gravar a sessão.\n\n" + ex.Message, "Salvar sessão", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ExportHistory()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar histórico",
            Filter = TextFilter,
            DefaultExt = ".txt",
            AddExtension = true,
            FileName = (Sessions.CurrentPath is null ? "historico" : Path.GetFileNameWithoutExtension(Sessions.CurrentPath)) + ".txt",
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            Sessions.Export(dialog.FileName);
            StatusMessage = "Histórico exportado.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("Não foi possível exportar.\n\n" + ex.Message, "Exportar histórico", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Limpa apenas o que está visível; o histórico da sessão permanece.</summary>
    [RelayCommand]
    private void ClearDisplay()
    {
        HexPane.ClearLines();
        DecimalPane.ClearLines();
    }

    [RelayCommand]
    private void Exit() => Application.Current.MainWindow?.Close();

    /// <summary>A janela principal assina este evento e abre o diálogo de variáveis (a VM não conhece janelas).</summary>
    public event EventHandler? ShowVariablesRequested;

    [RelayCommand]
    private void ShowVariables() => ShowVariablesRequested?.Invoke(this, EventArgs.Empty);

    public event EventHandler? ShowHelpRequested;

    [RelayCommand]
    private void ShowHelp() => ShowHelpRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void ClearVariables()
    {
        if (Session.Variables.Count == 0) return;

        var answer = MessageBox.Show(
            "Remover todas as variáveis? O histórico do display é mantido.",
            "Limpar variáveis", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Yes)
            Session.Variables.Clear();
    }

    [RelayCommand]
    private void ShowAbout() => MessageBox.Show(
        "CalculadoraHex\n\nCalculadora hexadecimal/decimal com painéis sincronizados, variáveis e histórico automático.\n\n" +
        "Sintaxe: [nome =] expressão, com + - * / e parênteses.\n" +
        "No painel hexa, 'a'..'f' são variáveis se definidas, senão dígitos. Use 0x para forçar número e $ para forçar variável.\n\n" +
        $"As sessões são gravadas automaticamente em:\n{Sessions.SessionsDirectory}",
        "Sobre", MessageBoxButton.OK, MessageBoxImage.Information);

    private void UpdateSessionName()
    {
        SessionName = Sessions.CurrentPath is null
            ? "Sessão nova (não gravada)"
            : "Sessão: " + Path.GetFileNameWithoutExtension(Sessions.CurrentPath);
    }

    private void UpdateVariablesSummary()
    {
        var vars = Session.Variables.Snapshot();
        if (vars.Count == 0)
        {
            VariablesSummary = "Nenhuma variável definida";
            return;
        }

        VariablesSummary = string.Join("    ", vars
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"{kv.Key} = {NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal)}h ({NumberFormatter.Format(kv.Value, NumberBase.Decimal)})"));
    }

    private static string EnsureDirectory(string directory)
    {
        try { Directory.CreateDirectory(directory); } catch (IOException) { }
        return directory;
    }
}
