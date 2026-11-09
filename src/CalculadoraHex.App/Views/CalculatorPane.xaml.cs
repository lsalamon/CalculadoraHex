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
