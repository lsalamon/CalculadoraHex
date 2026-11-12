using System.Numerics;
using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.Core.Tests;

public class RootVariablesTests
{
    [Fact]
    public void Conta_inexata_gera_seis_nomes_e_a_exata_gera_tres()
    {
        var inexata = RootVariables.Build("a", IntegerRoot.Compute(1234, 3), 1);
        Assert.Equal(
            new[] { "a_raiz", "a_pot", "a_dif", "a_prox", "a_proxpot", "a_exc" },
            inexata.Select(v => v.Name));
        Assert.Equal(
            new BigInteger[] { 10, 1000, 234, 11, 1331, 97 },
            inexata.Select(v => v.Value));

        var exata = RootVariables.Build("b", IntegerRoot.Compute(1000, 3), 1);
        Assert.Equal(new[] { "b_raiz", "b_pot", "b_dif" }, exata.Select(v => v.Name));
    }

    [Fact]
    public void Sufixo_um_nao_aparece_e_os_demais_vao_no_fim_de_cada_nome()
    {
        var r = IntegerRoot.Compute(1234, 3);

        Assert.All(RootVariables.Build("a", r, 1), v => Assert.DoesNotMatch(@"\d$", v.Name));
        Assert.Equal(
            new[] { "a_raiz4", "a_pot4", "a_dif4", "a_prox4", "a_proxpot4", "a_exc4" },
            RootVariables.Build("a", r, 4).Select(v => v.Name));
    }

    [Fact]
    public void Escolhe_o_menor_sufixo_livre_considerando_so_os_nomes_que_serao_criados()
    {
        var inexata = IntegerRoot.Compute(1234, 3);
        var exata = IntegerRoot.Compute(1000, 3);
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_raiz", "a_exc", "a_raiz2" };

        Assert.Equal(3, RootVariables.ChooseSuffix("a", inexata, definidas.Contains));
        Assert.Equal(1, RootVariables.ChooseSuffix("b", exata, definidas.Contains));
        // "a_exc" não faz parte de um resultado exato, então só "a_raiz" impede o sufixo 1
        Assert.Equal(3, RootVariables.ChooseSuffix("a", exata, definidas.Contains));
    }

    [Fact]
    public void Nomes_nao_diferenciam_maiusculas_ao_escolher_o_sufixo()
    {
        var r = IntegerRoot.Compute(1234, 3);
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A_RAIZ" };

        Assert.Equal(2, RootVariables.ChooseSuffix("a", r, definidas.Contains));
    }
}
