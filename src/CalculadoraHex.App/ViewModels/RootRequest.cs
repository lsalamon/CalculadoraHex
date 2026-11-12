namespace CalculadoraHex.App.ViewModels;

/// <summary>Escolha feita no diálogo de raiz inteira: nome da variável perseguida e quantidade de fatores n.</summary>
public sealed record RootRequest(string VariableName, int Exponent);
