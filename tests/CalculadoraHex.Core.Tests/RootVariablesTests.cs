using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.Core.Tests;

public class RootVariablesTests
{
    [Fact]
    public void Sao_tres_variaveis_novas_raiz_result_e_dif()
    {
        var names = RootVariables.Names("v1", 1);

        Assert.Equal("v1_raiz", names.Root);
        Assert.Equal("v1_result", names.Result);
        Assert.Equal("v1_dif", names.Difference);
    }

    [Fact]
    public void Sufixo_um_nao_aparece_e_os_demais_vao_no_fim_de_cada_nome()
    {
        var names = RootVariables.Names("v1", 4);

        Assert.Equal("v1_raiz4", names.Root);
        Assert.Equal("v1_result4", names.Result);
        Assert.Equal("v1_dif4", names.Difference);
    }

    [Fact]
    public void Escolhe_o_menor_sufixo_livre_para_o_conjunto()
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_result", "a_dif2", "a_result2" };

        Assert.Equal(3, RootVariables.ChooseSuffix("a", definidas.Contains));
        Assert.Equal(1, RootVariables.ChooseSuffix("b", definidas.Contains));
    }

    [Fact]
    public void Basta_um_dos_tres_nomes_estar_definido_para_trocar_de_sufixo()
    {
        var soDif = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_dif" };
        var soRaiz = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_raiz" };

        Assert.Equal(2, RootVariables.ChooseSuffix("a", soDif.Contains));
        Assert.Equal(2, RootVariables.ChooseSuffix("a", soRaiz.Contains));
    }

    [Fact]
    public void Nomes_nao_diferenciam_maiusculas_ao_escolher_o_sufixo()
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A_RESULT" };

        Assert.Equal(2, RootVariables.ChooseSuffix("a", definidas.Contains));
    }
}
