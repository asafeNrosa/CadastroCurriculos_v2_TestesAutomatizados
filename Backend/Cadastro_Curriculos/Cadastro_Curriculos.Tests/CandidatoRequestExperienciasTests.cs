using System.ComponentModel.DataAnnotations;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests;

public class CandidatoRequestExperienciasTests
{
    private static List<ValidationResult> Validar(CandidatoRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static CandidatoRequest CriarComExperiencias(int quantidade) => new()
    {
        NomeCompleto = "Maria Eduarda Ferreira",
        Email = "maria.ferreira@exemplo.com",
        Experiencias = Enumerable.Range(1, quantidade)
            .Select(i => new ExperienciaRequest
            {
                Empresa = $"Empresa {i}",
                Cargo = "Desenvolvedor",
                DataInicio = new DateOnly(2020, 1, 1).AddMonths(i)
            })
            .ToList()
    };

    [Fact]
    public void Experiencias_PorPadraoComeçaVazia_ECadastroSemExperienciasEhValido()
    {
        var request = new CandidatoRequest { NomeCompleto = "Maria", Email = "maria@exemplo.com" };

        Assert.Empty(request.Experiencias);
        Assert.Empty(Validar(request));
    }

    [Fact]
    public void Validar_ComUmaExperiencia_NaoRetornaErros()
    {
        Assert.Empty(Validar(CriarComExperiencias(1)));
    }

    [Fact]
    public void Validar_ComExatamenteOLimiteDeExperiencias_NaoRetornaErros()
    {
        Assert.Empty(Validar(CriarComExperiencias(CandidatoRequest.MaximoDeExperiencias)));
    }

    [Fact]
    public void Validar_ComUmaExperienciaAcimaDoLimite_RetornaErro()
    {
        var erros = Validar(CriarComExperiencias(CandidatoRequest.MaximoDeExperiencias + 1));

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(CandidatoRequest.Experiencias)) &&
            e.ErrorMessage == $"Informe no máximo {CandidatoRequest.MaximoDeExperiencias} experiências profissionais.");
    }
}
