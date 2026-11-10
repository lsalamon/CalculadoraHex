using System.Windows;
using CalculadoraHex.Core;

namespace CalculadoraHex.App.Views;

/// <summary>Pergunta em que base ler as linhas sem prefixo de um script importado.</summary>
public partial class ImportBaseWindow : Window
{
    public ImportBaseWindow(NumberBase suggested)
    {
        InitializeComponent();
        if (suggested == NumberBase.Hexadecimal) HexOption.IsChecked = true;
        else DecOption.IsChecked = true;
    }

    public NumberBase SelectedBase => HexOption.IsChecked == true ? NumberBase.Hexadecimal : NumberBase.Decimal;

    private void OnImportClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
