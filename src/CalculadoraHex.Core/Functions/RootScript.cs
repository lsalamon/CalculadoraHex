using CalculadoraHex.Core.Formatting;

namespace CalculadoraHex.Core.Functions;

/// <summary>
/// As linhas que o resultado de uma raiz inteira "digita" na calculadora, como se o usuário fizesse a entrada dos dados:
/// um comentário dizendo de qual variável e de qual n se trata, e uma atribuição por valor. Cada linha é escrita na base
/// do painel em que será digitada, então avaliá-la ali dá exatamente o valor pretendido.
/// </summary>
public static class RootScript
{
    public static IReadOnlyList<string> Build(string source, IntegerRootResult result, int suffix, NumberBase typedBase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(result);

        var lines = new List<string> { $"#raiz {result.N} de {source}" };
        foreach (var variable in RootVariables.Build(source, result, suffix))
            lines.Add($"{variable.Name} = {NumberFormatter.FormatForEntry(variable.Value, typedBase)}");
        return lines;
    }
}
