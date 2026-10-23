using System.ComponentModel.DataAnnotations;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests;

public class ExperienciaRequestValidacaoTests
{
    private static List<ValidationResult> Validar(ExperienciaRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static ExperienciaRequest CriarValida() => new()
    {
        Empresa = "Acme Tecnologia",
        Cargo = "Desenvolvedor Backend",
        DataInicio = new DateOnly(2023, 2, 1)
    };

    [Fact]
    public void Validar_ApenasCamposObrigatorios_NaoRetornaErros()
    {
        Assert.Empty(Validar(CriarValida()));
    }

    [Fact]
    public void Validar_ComTerminoEDescricao_NaoRetornaErros()
    {
        var request = CriarValida();
        request.DataFim = new DateOnly(2024, 6, 30);
        request.Descricao = "APIs REST em ASP.NET Core.";

        Assert.Empty(Validar(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_EmpresaVazia_RetornaErroDeEmpresaObrigatoria(string empresa)
    {
        var request = CriarValida();
        request.Empresa = empresa;

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(ExperienciaRequest.Empresa)) &&
            e.ErrorMessage == "A empresa é obrigatória.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_CargoVazio_RetornaErroDeCargoObrigatorio(string cargo)
    {
        var request = CriarValida();
        request.Cargo = cargo;

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(ExperienciaRequest.Cargo)) &&
            e.ErrorMessage == "O cargo é obrigatório.");
    }

    [Fact]
    public void Validar_SemDataDeInicio_RetornaErroDeDataObrigatoria()
    {
        var request = CriarValida();
        request.DataInicio = null;

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(ExperienciaRequest.DataInicio)) &&
            e.ErrorMessage == "A data de início é obrigatória.");
    }

    [Fact]
    public void Validar_TerminoAnteriorAoInicio_RetornaErroNaDataFim()
    {
        var request = CriarValida();
        request.DataInicio = new DateOnly(2024, 5, 1);
        request.DataFim = new DateOnly(2024, 1, 1);

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(ExperienciaRequest.DataFim)) &&
            e.ErrorMessage == "A data de término não pode ser anterior à data de início.");
    }

    [Fact]
    public void Validar_TerminoNoMesmoDiaDoInicio_EhValido()
    {
        var request = CriarValida();
        request.DataInicio = new DateOnly(2024, 5, 1);
        request.DataFim = new DateOnly(2024, 5, 1);

        Assert.Empty(Validar(request));
    }

    [Fact]
    public void Validar_EmpregoAtualSemTermino_EhValido()
    {
        var request = CriarValida();
        request.DataFim = null;

        Assert.Empty(Validar(request));
    }

    [Fact]
    public void Validar_EmpresaComMaisDe150Caracteres_RetornaErro()
    {
        var request = CriarValida();
        request.Empresa = new string('A', 151);

        Assert.Contains(Validar(request), e => e.MemberNames.Contains(nameof(ExperienciaRequest.Empresa)));
    }

    [Fact]
    public void Validar_DescricaoComMaisDe1000Caracteres_RetornaErro()
    {
        var request = CriarValida();
        request.Descricao = new string('D', 1001);

        Assert.Contains(Validar(request), e => e.MemberNames.Contains(nameof(ExperienciaRequest.Descricao)));
    }
}
