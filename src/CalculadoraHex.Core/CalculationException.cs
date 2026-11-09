namespace CalculadoraHex.Core;

/// <summary>Erro de sintaxe ou de avaliação em uma linha digitada pelo usuário.</summary>
public sealed class CalculationException : Exception
{
    public CalculationException(string message, int position = -1) : base(message)
    {
        Position = position;
    }

    /// <summary>Posição (índice de caractere) onde o erro foi detectado, ou -1 se não se aplica.</summary>
    public int Position { get; }
}
