using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using CalculadoraHex.App.ViewModels;

namespace CalculadoraHex.App.Views;

public partial class CalculatorPane : UserControl
{
    private PaneViewModel? _viewModel;

    public CalculatorPane()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public void FocusInput() => InputBox.Focus();

    /// <summary>Disparado quando a linha de entrada deste painel recebe o foco (para o menu Operações saber onde inserir).</summary>
    public event EventHandler? InputFocused;

    /// <summary>Insere texto na posição do cursor da linha de entrada e devolve o foco a ela.</summary>
    public void InsertText(string text)
    {
        var start = InputBox.SelectionStart;
        var current = InputBox.Text ?? string.Empty;
        InputBox.Text = current.Remove(start, InputBox.SelectionLength).Insert(start, text);
        InputBox.SelectionStart = start + text.Length;
        InputBox.SelectionLength = 0;
        InputBox.Focus();
    }

    public void MoveCaret(int delta)
    {
        var position = Math.Clamp(InputBox.SelectionStart + delta, 0, InputBox.Text.Length);
        InputBox.SelectionStart = position;
        InputBox.SelectionLength = 0;
    }

    private void OnInputGotFocus(object sender, RoutedEventArgs e) => InputFocused?.Invoke(this, EventArgs.Empty);

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.Lines.CollectionChanged -= OnLinesChanged;

        _viewModel = e.NewValue as PaneViewModel;

        if (_viewModel is not null)
            _viewModel.Lines.CollectionChanged += OnLinesChanged;
    }

    /// <summary>Mantém o display rolado para a última linha, como uma calculadora de fita.</summary>
    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
            Dispatcher.BeginInvoke(Scroller.ScrollToEnd, System.Windows.Threading.DispatcherPriority.Loaded);
    }
}
