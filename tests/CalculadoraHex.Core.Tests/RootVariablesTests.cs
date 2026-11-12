using CalculadoraHex.Core.Functions;

namespace CalculadoraHex.Core.Tests;

public class RootVariablesTests
{
    [Fact]
    public void Sao_tres_variaveis_novas_e_o_numero_do_calculo_vai_no_fim_de_cada_nome()
    {
        var names = RootVariables.Names("v1", 1);

        Assert.Equal("v1_raiz1", names.Root);
        Assert.Equal("v1_result1", names.Result);
        Assert.Equal("v1_dif1", names.Difference);
    }

    [Fact]
    public void Outros_numeros_tambem_vao_no_fim_de_cada_nome()
    {
        var names = RootVariables.Names("v1", 4);

        Assert.Equal("v1_raiz4", names.Root);
        Assert.Equal("v1_result4", names.Result);
        Assert.Equal("v1_dif4", names.Difference);
    }

    [Fact]
    public void O_primeiro_calculo_usa_1_e_os_seguintes_o_proximo_numero_livre()
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(1, RootVariables.ChooseSuffix("a", definidas.Contains));

        foreach (var expected in new[] { 1, 2, 3 })
        {
            var n = RootVariables.ChooseSuffix("a", definidas.Contains);
            Assert.Equal(expected, n);
            foreach (var name in RootVariables.Names("a", n).All) definidas.Add(name);
        }
    }

    [Fact]
    public void Pula_os_numeros_cujos_nomes_ja_existem()
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_result1", "a_dif2", "a_result2" };

        Assert.Equal(3, RootVariables.ChooseSuffix("a", definidas.Contains));
        Assert.Equal(1, RootVariables.ChooseSuffix("b", definidas.Contains));
    }

    [Fact]
    public void Basta_um_dos_tres_nomes_estar_definido_para_trocar_de_numero()
    {
        var soDif = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_dif1" };
        var soRaiz = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_raiz1" };

        Assert.Equal(2, RootVariables.ChooseSuffix("a", soDif.Contains));
        Assert.Equal(2, RootVariables.ChooseSuffix("a", soRaiz.Contains));
    }

    [Fact]
    public void Nomes_nao_diferenciam_maiusculas_ao_escolher_o_numero()
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A_RESULT1" };

        Assert.Equal(2, RootVariables.ChooseSuffix("a", definidas.Contains));
    }

    [Theory]
    [InlineData("v1_raiz1")]
    [InlineData("v1_result3")]
    [InlineData("v1_dif12")]
    [InlineData("V1_RAIZ1")]
    public void Raiz_de_uma_variavel_criada_por_outra_raiz_continua_a_numeracao_da_origem(string origem)
    {
        var definidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "v1_raiz1", "v1_result1", "v1_dif1", origem,
        };

        var n = RootVariables.ChooseSuffix(origem, definidas.Contains);
        var names = RootVariables.Names(origem, n);

        Assert.Equal(2, n);
        Assert.Equal(origem.StartsWith("V1") ? "V1_raiz2" : "v1_raiz2", names.Root, ignoreCase: true);
        Assert.Equal("v1_result2", names.Result, ignoreCase: true);
        Assert.Equal("v1_dif2", names.Difference, ignoreCase: true);
    }

    [Theory]
    [InlineData("v1", "v1")]
    [InlineData("valor_raiz", "valor_raiz")]     // sem número no fim: nome comum
    [InlineData("raiz1", "raiz1")]                // sem a parte antes do sublinhado
    [InlineData("_raiz1", "_raiz1")]
    [InlineData("v1_raiz1_raiz1", "v1_raiz1")]   // só o último trecho gerado sai
    public void Nome_base_so_perde_o_trecho_gerado_no_fim(string origem, string esperado)
    {
        Assert.Equal(esperado, RootVariables.BaseName(origem));
    }
}
