using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CalculadoraHex.App.Services;
using CalculadoraHex.App.ViewModels;

namespace CalculadoraHex.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    private readonly UserPreferences _preferences = UserPreferences.Load();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.ShowVariablesRequested += (_, _) => ShowVariablesWindow();

        if (_preferences.WindowWidth is > 300) Width = _preferences.WindowWidth.Value;
        if (_preferences.WindowHeight is > 200) Height = _preferences.WindowHeight.Value;

        // Restaura a visibilidade dos painéis antes de assinar o evento, para não regravar durante o load.
        _viewModel.HexPane.IsVisible = _preferences.HexPaneVisible || !_preferences.DecimalPaneVisible;
        _viewModel.DecimalPane.IsVisible = _preferences.DecimalPaneVisible;
        ApplyPaneVisibility();

        _viewModel.HexPane.PropertyChanged += OnPanePropertyChanged;
        _viewModel.DecimalPane.PropertyChanged += OnPanePropertyChanged;

        Loaded += (_, _) => FocusFirstVisiblePane();
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _preferences.HexPaneVisible = _viewModel.HexPane.IsVisible;
        _preferences.DecimalPaneVisible = _viewModel.DecimalPane.IsVisible;
        if (WindowState == WindowState.Normal)
        {
            _preferences.WindowWidth = Width;
            _preferences.WindowHeight = Height;
        }
        _preferences.Save();
    }

    private void ShowVariablesWindow()
    {
        var window = new VariablesWindow(new VariablesViewModel(_viewModel.Session.Variables)) { Owner = this };
        window.ShowDialog();
        FocusFirstVisiblePane();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        switch (e.Key)
        {
            case Key.D1:
                _viewModel.HexPane.IsVisible = !_viewModel.HexPane.IsVisible;
                e.Handled = true;
                break;
            case Key.D2:
                _viewModel.DecimalPane.IsVisible = !_viewModel.DecimalPane.IsVisible;
                e.Handled = true;
                break;
        }
    }

    private void OnPanePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PaneViewModel.IsVisible)) return;

        // Nunca deixa os dois ocultos: desfaz o pedido que acabou de chegar.
        if (!_viewModel.HexPane.IsVisible && !_viewModel.DecimalPane.IsVisible && sender is PaneViewModel pane)
        {
            pane.IsVisible = true;
            return;
        }

        ApplyPaneVisibility();
        _preferences.HexPaneVisible = _viewModel.HexPane.IsVisible;
        _preferences.DecimalPaneVisible = _viewModel.DecimalPane.IsVisible;
        _preferences.Save();
    }

    /// <summary>
    /// Oculta/exibe colunas. O histórico é compartilhado, então um painel reexibido já traz todas as operações.
    /// </summary>
    private void ApplyPaneVisibility()
    {
        var hex = _viewModel.HexPane.IsVisible;
        var dec = _viewModel.DecimalPane.IsVisible;

        HexPaneView.Visibility = hex ? Visibility.Visible : Visibility.Collapsed;
        DecimalPaneView.Visibility = dec ? Visibility.Visible : Visibility.Collapsed;
        Splitter.Visibility = hex && dec ? Visibility.Visible : Visibility.Collapsed;

        HexColumn.Width = hex ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        DecimalColumn.Width = dec ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        if (IsLoaded) FocusFirstVisiblePane();
    }

    private void FocusFirstVisiblePane()
    {
        if (_viewModel.HexPane.IsVisible) HexPaneView.FocusInput();
        else DecimalPaneView.FocusInput();
    }
}
