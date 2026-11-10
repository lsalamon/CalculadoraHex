using System.Windows;
using CalculadoraHex.App.ViewModels;

namespace CalculadoraHex.App.Views;

public partial class VariablesWindow : Window
{
    public VariablesWindow(VariablesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnRemoveAllClick(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Remover todas as variáveis? O histórico do display é mantido.",
            "Limpar variáveis", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes && DataContext is VariablesViewModel vm)
            vm.RemoveAllCommand.Execute(null);
    }
}
