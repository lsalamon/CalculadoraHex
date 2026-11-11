using System.IO;
using System.Windows.Threading;
using CalculadoraHex.Core.Evaluation;
using CalculadoraHex.Core.Persistence;

namespace CalculadoraHex.App.Services;

/// <summary>
/// Liga a <see cref="CalculationSession"/> ao disco: grava automaticamente (com debounce) a cada mudança,
/// reabre a última sessão, e atende Abrir / Salvar como / Exportar.
/// </summary>
public sealed class SessionManager
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(300);

    private readonly CalculationSession _session;
    private readonly SessionStore _store;
    private readonly DispatcherTimer _timer;
    private bool _suspended;
    private bool _dirty;

    public SessionManager(CalculationSession session, SessionStore store)
    {
        _session = session;
        _store = store;

        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = SaveDelay };
        _timer.Tick += (_, _) => { _timer.Stop(); SaveNow(); };

        _session.EntryAdded += (_, _) => ScheduleSave();
        _session.Reset += (_, _) => ScheduleSave();
        _session.HistoryReplaced += (_, _) => ScheduleSave();
        _session.Variables.Changed += (_, _) => ScheduleSave();
    }

    /// <summary>Arquivo da sessão atual; nulo até a primeira gravação de uma sessão nova.</summary>
    public string? CurrentPath { get; private set; }

    public event EventHandler? CurrentPathChanged;

    /// <summary>Erro de disco na gravação automática, para a UI avisar sem interromper o uso.</summary>
    public event EventHandler<Exception>? SaveFailed;

    public string SessionsDirectory => _store.Directory;

    /// <summary>Reabre a última sessão gravada, se existir. Retorna falso se não havia ou falhou.</summary>
    public bool TryRestoreLast()
    {
        var path = _store.GetLastSessionPath();
        if (path is null) return false;

        try
        {
            Open(path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException)
        {
            return false;
        }
    }

    public void Open(string path)
    {
        _suspended = true;
        try
        {
            _store.LoadInto(_session, path);
        }
        finally
        {
            _suspended = false;
        }

        _timer.Stop();
        _dirty = false;
        SetCurrentPath(path);
        _store.SetLastSessionPath(path);
    }

    /// <summary>Zera a sessão; o novo arquivo só é criado na primeira linha digitada.</summary>
    public void StartNew()
    {
        FlushSave();
        _suspended = true;
        try
        {
            _session.Clear();
        }
        finally
        {
            _suspended = false;
        }
        _dirty = false;
        SetCurrentPath(null);
        _store.SetLastSessionPath(null);
    }

    public void SaveAs(string path)
    {
        SetCurrentPath(path);
        SaveNow();
    }

    public void Export(string path) => File.WriteAllText(path, SessionStore.ExportText(_session));

    /// <summary>Grava imediatamente o que estiver pendente (usado ao fechar a janela).</summary>
    public void FlushSave()
    {
        if (!_timer.IsEnabled && !_dirty) return;
        _timer.Stop();
        SaveNow();
    }

    private void ScheduleSave()
    {
        if (_suspended) return;
        _dirty = true;
        _timer.Stop();
        _timer.Start();
    }

    private void SaveNow()
    {
        // Sessão nova e ainda vazia: nada a gravar.
        if (CurrentPath is null && _session.Entries.Count == 0)
        {
            _dirty = false;
            return;
        }

        try
        {
            var created = CurrentPath is null;
            CurrentPath ??= _store.NewSessionPath(_session.CreatedAt);
            _store.Save(_session, CurrentPath);
            _store.SetLastSessionPath(CurrentPath);
            _dirty = false;
            if (created) CurrentPathChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SaveFailed?.Invoke(this, ex);
        }
    }

    private void SetCurrentPath(string? path)
    {
        if (string.Equals(CurrentPath, path, StringComparison.OrdinalIgnoreCase)) return;
        CurrentPath = path;
        CurrentPathChanged?.Invoke(this, EventArgs.Empty);
    }
}
