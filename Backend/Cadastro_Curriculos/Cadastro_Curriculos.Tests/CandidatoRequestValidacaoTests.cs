using System.ComponentModel.DataAnnotations;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests;

public class CandidatoRequestValidacaoTests
{
    private static List<ValidationResult> Validar(CandidatoRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static CandidatoRequest CriarValido() => new()
    {
        NomeCompleto = "Maria Eduarda Ferreira",
        Email = "maria.ferreira@exemplo.com"
    };

    [Fact]
    public void Validar_ApenasCamposObrigatoriosPreenchidos_NaoRetornaErros()
    {
        var erros = Validar(CriarValido());

        Assert.Empty(erros);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_NomeVazio_RetornaErroDeNomeObrigatorio(string nome)
    {
        var request = CriarValido();
        request.NomeCompleto = nome;

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(CandidatoRequest.NomeCompleto)) &&
            e.ErrorMessage == "O nome completo é obrigatório.");
    }

    [Fact]
    public void Validar_NomeAcimaDe150Caracteres_RetornaErro()
    {
        var request = CriarValido();
        request.NomeCompleto = new string('a', 151);

        var erros = Validar(request);

        Assert.Contains(erros, e => e.MemberNames.Contains(nameof(CandidatoRequest.NomeCompleto)));
    }

    [Fact]
    public void Validar_EmailVazio_RetornaErroDeEmailObrigatorio()
    {
        var request = CriarValido();
        request.Email = "";

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(CandidatoRequest.Email)) &&
            e.ErrorMessage == "O e-mail é obrigatório.");
    }

    [Theory]
    [InlineData("email-invalido")]
    [InlineData("joao@empresa")]
    [InlineData("joao@@exemplo.com")]
    [InlineData("joao souza@exemplo.com")]
    public void Validar_EmailComFormatoInvalido_RetornaErroDeFormato(string email)
    {
        var request = CriarValido();
        request.Email = email;

        var erros = Validar(request);

        Assert.Contains(erros, e =>
            e.MemberNames.Contains(nameof(CandidatoRequest.Email)) &&
            e.ErrorMessage == "Informe um e-mail válido.");
    }
}