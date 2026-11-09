using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CalculadoraHex.App.ViewModels;

namespace CalculadoraHex.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.HexPane.PropertyChanged += OnPanePropertyChanged;
        _viewModel.DecimalPane.PropertyChanged += OnPanePropertyChanged;

        Loaded += (_, _) => HexPaneView.FocusInput();
        PreviewKeyDown += OnPreviewKeyDown;
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

        if (hex && !dec) HexPaneView.FocusInput();
        else if (dec && !hex) DecimalPaneView.FocusInput();
    }
}
