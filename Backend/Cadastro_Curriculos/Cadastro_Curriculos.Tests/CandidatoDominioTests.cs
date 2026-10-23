using CadastroCurriculos.Api.Models;

namespace Cadastro_Curriculos.Tests;

public class CandidatoDominioTests
{
    [Fact]
    public void PertenceA_UsuarioDono_RetornaVerdadeiro()
    {
        var candidato = new Candidato { UsuarioId = 7 };

        Assert.True(candidato.PertenceA(7));
    }

    [Fact]
    public void PertenceA_OutroUsuario_RetornaFalso()
    {
        var candidato = new Candidato { UsuarioId = 7 };

        Assert.False(candidato.PertenceA(8));
    }

    [Fact]
    public void PertenceA_VisitanteSemLogin_RetornaFalso()
    {
        var candidato = new Candidato { UsuarioId = 7 };

        Assert.False(candidato.PertenceA(null));
    }

    [Fact]
    public void PertenceA_CadastroAnonimo_NuncaPertenceAninguem()
    {
        // Sem dono (UsuarioId nulo), nem o visitante sem login (null == null) nem um usuário logado podem alterar.
        var candidato = new Candidato { UsuarioId = null };

        Assert.False(candidato.PertenceA(null));
        Assert.False(candidato.PertenceA(7));
    }

    [Fact]
    public void Experiencias_ComecaVazia()
    {
        Assert.Empty(new Candidato().Experiencias);
    }

    [Fact]
    public void EmAndamento_SemDataDeTermino_RetornaVerdadeiro()
    {
        var experiencia = new ExperienciaProfissional { DataInicio = new DateOnly(2024, 1, 1), DataFim = null };

        Assert.True(experiencia.EmAndamento);
    }

    [Fact]
    public void EmAndamento_ComDataDeTermino_RetornaFalso()
    {
        var experiencia = new ExperienciaProfissional
        {
            DataInicio = new DateOnly(2024, 1, 1),
            DataFim = new DateOnly(2024, 6, 30)
        };

        Assert.False(experiencia.EmAndamento);
    }
}
