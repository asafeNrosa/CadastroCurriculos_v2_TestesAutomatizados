using Cadastro_Curriculos.Services;
using CadastroCurriculos.Api.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Cadastro_Curriculos.Tests;

public class GeradorDeTokenTests
{
    private const string Chave = "chave-somente-para-testes-unitarios-1234567890";

    private static JwtOptions Opcoes(string chave = Chave) => new()
    {
        Chave = chave,
        Emissor = "emissor-teste",
        Audiencia = "audiencia-teste",
        ExpiraEmMinutos = 120
    };

    private static Usuario UsuarioDeTeste() => new() { Id = 42, Email = "maria@exemplo.com" };

    private static TokenValidationParameters ParametrosDeValidacao(string chave) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = "emissor-teste",
        ValidateAudience = true,
        ValidAudience = "audiencia-teste",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    [Fact]
    public void Gerar_TokenTemOIdDoUsuarioNoClaimSubEOEmail()
    {
        var (token, _) = new GeradorDeToken(Opcoes()).Gerar(UsuarioDeTeste());

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal("42", jwt.Subject);
        Assert.Equal("maria@exemplo.com", jwt.GetClaim("email").Value);
    }

    [Fact]
    public void Gerar_TokenTemEmissorEAudienciaConfigurados()
    {
        var (token, _) = new GeradorDeToken(Opcoes()).Gerar(UsuarioDeTeste());

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal("emissor-teste", jwt.Issuer);
        Assert.Contains("audiencia-teste", jwt.Audiences);
    }

    [Fact]
    public void Gerar_ExpiraNoPrazoConfigurado()
    {
        var agora = new DateTime(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

        var (token, expiraEm) = new GeradorDeToken(Opcoes()).Gerar(UsuarioDeTeste(), agora);

        Assert.Equal(agora.AddMinutes(120), expiraEm);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        Assert.True(Math.Abs((jwt.ValidTo - expiraEm).TotalSeconds) < 1);
    }

    [Fact]
    public async Task Gerar_TokenEhValidoParaAMesmaChave()
    {
        var (token, _) = new GeradorDeToken(Opcoes()).Gerar(UsuarioDeTeste());

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, ParametrosDeValidacao(Chave));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Gerar_TokenEhRejeitadoComOutraChave()
    {
        var (token, _) = new GeradorDeToken(Opcoes()).Gerar(UsuarioDeTeste());

        var resultado = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, ParametrosDeValidacao("outra-chave-completamente-diferente-1234567890"));

        Assert.False(resultado.IsValid);
    }
}
