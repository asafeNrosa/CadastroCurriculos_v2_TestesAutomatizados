using System.ComponentModel.DataAnnotations;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests;

public class RegistrarRequestValidacaoTests
{
    private static List<ValidationResult> Validar(RegistrarRequest request)
    {
        var erros = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), erros, validateAllProperties: true);
        return erros;
    }

    private static RegistrarRequest CriarValido() => new() { Email = "ana.lima@exemplo.com", Senha = "senha-segura-123" };

    [Fact]
    public void Validar_DadosValidos_NaoRetornaErros()
    {
        Assert.Empty(Validar(CriarValido()));
    }

    [Theory]
    [InlineData("sem-arroba")]
    [InlineData("@semlocal.com")]
    [InlineData("sem@dominio")]
    [InlineData("com espaco@exemplo.com")]
    public void Validar_EmailInvalido_RetornaErroDeEmailValido(string email)
    {
        var request = CriarValido();
        request.Email = email;

        Assert.Contains(Validar(request), e =>
            e.MemberNames.Contains(nameof(RegistrarRequest.Email)) &&
            e.ErrorMessage == "Informe um e-mail válido.");
    }

    [Fact]
    public void Validar_SenhaComMenosDe8Caracteres_RetornaErro()
    {
        var request = CriarValido();
        request.Senha = "1234567";

        Assert.Contains(Validar(request), e =>
            e.MemberNames.Contains(nameof(RegistrarRequest.Senha)) &&
            e.ErrorMessage == "A senha deve ter no mínimo 8 caracteres.");
    }

    [Fact]
    public void Validar_SenhaComExatamente8Caracteres_EhValida()
    {
        var request = CriarValido();
        request.Senha = "12345678";

        Assert.Empty(Validar(request));
    }

    [Fact]
    public void Validar_SenhaVazia_RetornaErroDeSenhaObrigatoria()
    {
        var request = CriarValido();
        request.Senha = "";

        Assert.Contains(Validar(request), e => e.ErrorMessage == "A senha é obrigatória.");
    }

    [Fact]
    public void Validar_SenhaComMaisDe100Caracteres_RetornaErro()
    {
        var request = CriarValido();
        request.Senha = new string('x', 101);

        Assert.Contains(Validar(request), e => e.MemberNames.Contains(nameof(RegistrarRequest.Senha)));
    }
}
