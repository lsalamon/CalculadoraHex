using System.IO;
using System.Windows;
using CalculadoraHex.App.Services;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Formatting;
using CalculadoraHex.Core.Functions;
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
            // Nova sessão ou sessão aberta no meio de uma digitação automática: o que faltava digitar não vale mais.
            _presentation?.Cancel();
            HexPane.ClearLines();
            DecimalPane.ClearLines();
        };
        Session.HistoryReplaced += (_, _) =>
        {
            HexPane.Rebuild(Session.Entries);
            DecimalPane.Rebuild(Session.Entries);
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

    public void Shutdown()
    {
        _presentation?.Cancel();
        Sessions.FlushSave();
    }

    /// <summary>Digitação automática em andamento (Especiais > Raiz inteira); nula quando não há.</summary>
    private CancellationTokenSource? _presentation;

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

    /// <summary>A janela decide a base das linhas sem prefixo (abre o diálogo); retorna nulo se cancelado.</summary>
    public Func<NumberBase, NumberBase?>? AskImportBase { get; set; }

    /// <summary>Base sugerida para o diálogo de importação: a do painel que está com o foco.</summary>
    public NumberBase ActiveBase { get; set; } = NumberBase.Hexadecimal;

    [RelayCommand]
    private void ImportHistory()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar histórico",
            Filter = "Sessão (.json) ou texto exportado (.txt)|*.json;*.txt|Sessão CalculadoraHex (*.json)|*.json|Texto / script (*.txt)|*.txt|Todos os arquivos (*.*)|*.*",
            InitialDirectory = EnsureDirectory(Sessions.SessionsDirectory),
            CheckFileExists = true,
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            IReadOnlyList<ImportItem> items;
            if (string.Equals(Path.GetExtension(dialog.FileName), SessionStore.FileExtension, StringComparison.OrdinalIgnoreCase))
            {
                items = HistoryImporter.FromDocument(new SessionStore(SessionStore.DefaultDirectory).Read(dialog.FileName));
            }
            else
            {
                var content = File.ReadAllText(dialog.FileName);
                items = HistoryImporter.ParseScript(content, ActiveBase);

                // Arquivo exportado pela própria app: toda linha já traz [hex]/[dec], então não há o que perguntar.
                if (items.Any(i => i.NeedsBase))
                {
                    var chosen = AskImportBase?.Invoke(ActiveBase);
                    if (chosen is null) return;
                    items = HistoryImporter.ParseScript(content, chosen.Value);
                }
            }

            if (items.Count == 0)
            {
                StatusMessage = "Nada para importar: o arquivo não tem operações.";
                return;
            }

            var result = HistoryImporter.ImportInto(Session, items);
            StatusMessage = result.Failed == 0
                ? $"Importadas {result.Imported} operações."
                : $"Importadas {result.Imported} operações, {result.Failed} com erro.";
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show("Não foi possível importar.\n\n" + ex.Message, "Importar histórico", MessageBoxButton.OK, MessageBoxImage.Error);
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

    /// <summary>Apaga a última linha do histórico (cálculo ou separador) e recalcula as variáveis.</summary>
    [RelayCommand]
    private void DeleteLastLine()
    {
        if (Session.Entries.Count == 0)
        {
            StatusMessage = "Não há linha para apagar.";
            return;
        }

        var last = Session.Entries[^1];
        Session.RemoveLast();
        StatusMessage = last.IsSeparator
            ? "Linha separadora apagada."
            : $"Linha apagada: {last.RawText}";
    }

    /// <summary>Insere uma linha tracejada separadora no fim do histórico.</summary>
    [RelayCommand]
    private void InsertSeparator()
    {
        Session.AddSeparator();
        StatusMessage = "Linha separadora inserida.";
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

    /// <summary>A janela decide variável e n (abre o diálogo); retorna nulo se cancelado.</summary>
    public Func<IReadOnlyDictionary<string, System.Numerics.BigInteger>, RootRequest?>? AskRootRequest { get; set; }

    /// <summary>Pede à janela que devolva o foco à linha de entrada (depois do aviso de falta de variável).</summary>
    public event EventHandler? FocusInputRequested;

    // Ritmo da digitação automática: rápido o bastante para não cansar, lento o bastante para acompanhar.
    private const int CharDelayMs = 18;
    private const int BeforeEnterMs = 140;
    private const int AfterResultMs = 220;

    // Comentários são longos e não têm resultado a esperar: saem em blocos de caracteres e com pausas curtas.
    private const int CommentChunk = 4;
    private const int CommentPauseMs = 80;

    /// <summary>
    /// Especiais > Raiz inteira: menor inteiro que, multiplicado por si próprio n vezes, não passa do valor
    /// de uma variável; informa a diferença quando a conta não fecha. Sem variável definida, pede que uma seja atribuída.
    /// Resolvida a conta, os valores são digitados na linha de entrada do painel ativo, um por vez, como se o usuário
    /// fizesse a entrada dos dados: cada linha é uma entrada comum e mostra o seu resultado antes da próxima.
    /// </summary>
    [RelayCommand]
    private async Task RootOfVariable()
    {
        if (Session.Variables.Count == 0)
        {
            MessageBox.Show(
                "Não há nenhuma variável definida.\n\nAtribua um valor a uma variável antes, por exemplo a=1000, e execute esta função de novo.",
                "Raiz inteira", MessageBoxButton.OK, MessageBoxImage.Information);
            FocusInputRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var request = AskRootRequest?.Invoke(Session.Variables.Snapshot());
        if (request is null) return;

        IntegerRootResult result;
        try
        {
            if (!Session.Variables.TryGet(request.VariableName, out var value))
                throw new CalculationException($"Variável '{request.VariableName}' não definida.");
            result = IntegerRoot.Compute(value, request.Exponent);
        }
        catch (CalculationException ex)
        {
            MessageBox.Show(ex.Message, "Raiz inteira", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pane = ChooseTypingPane();
        var suffix = RootVariables.ChooseSuffix(request.VariableName, result, Session.Variables.IsDefined);
        var lines = RootScript.Build(request.VariableName, result, suffix, pane.Base);
        var created = RootVariables.Build(request.VariableName, result, suffix).Count;

        FocusInputRequested?.Invoke(this, EventArgs.Empty);
        if (!await TypeLinesAsync(pane, lines))
        {
            StatusMessage = "Raiz interrompida antes de terminar.";
            return;
        }

        var root = NumberFormatter.Format(result.Root, NumberBase.Decimal);
        StatusMessage = (result.IsExact
            ? $"Raiz {result.N} de {request.VariableName}: exata, x = {root}."
            : $"Raiz {result.N} de {request.VariableName}: x = {root}, diferença {NumberFormatter.Format(result.Difference, NumberBase.Decimal)}.")
            + $" {created} variáveis criadas.";
    }

    /// <summary>O painel ativo, se estiver visível; senão o que estiver à mostra. É nele que as linhas são digitadas.</summary>
    private PaneViewModel ChooseTypingPane()
    {
        var preferred = ActiveBase == NumberBase.Hexadecimal ? HexPane : DecimalPane;
        if (preferred.IsVisible) return preferred;
        return HexPane.IsVisible ? HexPane : DecimalPane;
    }

    /// <summary>
    /// Digita cada linha na entrada do painel, caractere a caractere, e a envia como se o usuário tivesse apertado Enter.
    /// Retorna falso se foi interrompida (nova sessão, sessão aberta ou janela fechada) antes de terminar.
    /// </summary>
    private async Task<bool> TypeLinesAsync(PaneViewModel pane, IReadOnlyList<string> lines)
    {
        using var cts = new CancellationTokenSource();
        _presentation = cts;
        HexPane.IsInputLocked = true;
        DecimalPane.IsInputLocked = true;

        try
        {
            // O bloco abre com uma linha tracejada, para ficar destacado; não repete uma que já esteja por último.
            if (Session.AddSeparatorUnlessLast())
                await Task.Delay(AfterResultMs, cts.Token);

            foreach (var line in lines)
            {
                var isComment = line.StartsWith('#');
                var chunk = isComment ? CommentChunk : 1;

                pane.InputText = string.Empty;
                for (var i = 0; i < line.Length; i += chunk)
                {
                    pane.InputText += line.Substring(i, Math.Min(chunk, line.Length - i));
                    await Task.Delay(CharDelayMs, cts.Token);
                }

                await Task.Delay(isComment ? CommentPauseMs : BeforeEnterMs, cts.Token);
                cts.Token.ThrowIfCancellationRequested(); // a sessão pode ter sido zerada durante a espera
                Session.Evaluate(line, pane.Base);
                pane.InputText = string.Empty;

                await Task.Delay(isComment ? CommentPauseMs : AfterResultMs, cts.Token);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            pane.InputText = string.Empty;
            return false;
        }
        finally
        {
            _presentation = null;
            HexPane.IsInputLocked = false;
            DecimalPane.IsInputLocked = false;
        }
    }

    /// <summary>Imprime todas as variáveis no display, entre separadores (sem repetir o anterior se já houver um).</summary>
    [RelayCommand]
    private void PrintVariables()
    {
        var count = Session.PrintVariables();
        StatusMessage = count == 0
            ? "Nenhuma variável definida para imprimir."
            : count == 1 ? "1 variável impressa no display." : $"{count} variáveis impressas no display.";
    }

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

        // Funções como a raiz inteira criam várias variáveis de uma vez: a barra mostra as primeiras e conta o resto.
        const int MaxShown = 5;
        var shown = string.Join("    ", vars
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(MaxShown)
            .Select(kv => $"{kv.Key} = {NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal)}h ({NumberFormatter.Format(kv.Value, NumberBase.Decimal)})"));

        VariablesSummary = vars.Count > MaxShown
            ? $"{shown}    … e mais {vars.Count - MaxShown} (Ctrl+Shift+V lista todas)"
            : shown;
    }

    private static string EnsureDirectory(string directory)
    {
        try { Directory.CreateDirectory(directory); } catch (IOException) { }
        return directory;
    }
}
