using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using CalculadoraHex.App.ViewModels;
using CalculadoraHex.Core;
using CalculadoraHex.Core.Formatting;
using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.App.Views;

/// <summary>Escolhe a variável a perseguir e o n. Valida antes de fechar, mostrando o motivo na própria janela.</summary>
public partial class RootWindow : Window
{
    private readonly Dictionary<string, BigInteger> _values;

    public RootWindow(IReadOnlyDictionary<string, BigInteger> variables)
    {
        InitializeComponent();

        _values = new Dictionary<string, BigInteger>(variables, StringComparer.OrdinalIgnoreCase);
        VariableList.ItemsSource = variables
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new VariableRow(
                kv.Key,
                NumberFormatter.Format(kv.Value, NumberBase.Hexadecimal),
                NumberFormatter.Format(kv.Value, NumberBase.Decimal)))
            .ToList();
        VariableList.SelectedIndex = 0;

        Loaded += (_, _) =>
        {
            ExponentBox.Focus();
            ExponentBox.SelectAll();
        };
    }

    /// <summary>Preenchido quando a janela fecha com Calcular.</summary>
    public RootRequest? Request { get; private set; }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (VariableList.SelectedItem is not null) OnCalculateClick(sender, e);
    }

    private void OnCalculateClick(object sender, RoutedEventArgs e)
    {
        if (VariableList.SelectedItem is not VariableRow row)
        {
            ShowError("Escolha uma variável na lista.");
            return;
        }

        // n é uma contagem: sempre decimal, sem sinal.
        if (!int.TryParse(ExponentBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var n))
        {
            ShowError("Digite n como um número inteiro decimal, sem sinal. Ex.: 2 para quadrado, 3 para cubo.");
            ExponentBox.Focus();
            ExponentBox.SelectAll();
            return;
        }

        try
        {
            IntegerRoot.Validate(_values[row.Name], n);
        }
        catch (CalculationException ex)
        {
            ShowError(ex.Message);
            return;
        }

        Request = new RootRequest(row.Name, n);
        DialogResult = true;
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
