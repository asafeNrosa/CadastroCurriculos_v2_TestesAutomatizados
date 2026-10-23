using System.Net;
using System.Net.Http.Json;
using Cadastro_Curriculos.Dtos;
using Cadastro_Curriculos.Services;
using CadastroCurriculos.Api.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Cadastro_Curriculos.Tests.Integracao;

public class AuthApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _cliente = factory.CriarClienteApi();

    [Fact]
    public async Task Registrar_DadosValidos_Retorna201ComTokenEEmail()
    {
        var email = ApoioApi.EmailUnico();

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = email, Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var sessao = await resposta.Content.ReadFromJsonAsync<SessaoResponse>();
        Assert.False(string.IsNullOrWhiteSpace(sessao!.Token));
        Assert.Equal(email, sessao.Email);
        Assert.True(sessao.ExpiraEm > DateTime.UtcNow);
    }

    [Fact]
    public async Task Registrar_NaoGuardaASenhaEmTextoPuro()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var usuario = await factory.ConsultarBancoAsync(
            db => Task.FromResult(db.Usuarios.Single(u => u.Email == email)));

        Assert.NotEqual(ApoioApi.SenhaPadrao, usuario.SenhaHash);
        Assert.DoesNotContain(ApoioApi.SenhaPadrao, usuario.SenhaHash);
    }

    [Fact]
    public async Task Registrar_EmailDuplicado_Retorna409()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = email, Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_EmailDuplicadoComMaiusculas_Retorna409()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = email.ToUpperInvariant(), Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_SenhaCurta_Retorna400()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = ApoioApi.EmailUnico(), Senha = "1234567" });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Registrar_EmailInvalido_Retorna400()
    {
        var resposta = await _cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = "isto-nao-e-email", Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_CredenciaisCorretas_Retorna200ComToken()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var sessao = await resposta.Content.ReadFromJsonAsync<SessaoResponse>();
        Assert.False(string.IsNullOrWhiteSpace(sessao!.Token));
    }

    [Fact]
    public async Task Login_EmailEmOutraCaixa_Retorna200()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = $"  {email.ToUpperInvariant()}  ", Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var resposta = await _cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Senha = "senha-errada-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Login_EmailInexistente_Retorna401ComAMesmaMensagemDaSenhaErrada()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.RegistrarAsync(_cliente, email);

        var senhaErrada = await _cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Senha = "senha-errada-123" });
        var inexistente = await _cliente.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = ApoioApi.EmailUnico("fantasma"), Senha = ApoioApi.SenhaPadrao });

        Assert.Equal(HttpStatusCode.Unauthorized, inexistente.StatusCode);
        // Não revela se o e-mail tem conta: as duas respostas são idênticas.
        Assert.Equal(Titulo(await senhaErrada.Content.ReadAsStringAsync()),
                     Titulo(await inexistente.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Token_Valido_EhAceitoEmRotaProtegida()
    {
        var sessao = await ApoioApi.RegistrarAsync(_cliente);
        var logado = factory.CriarClienteApi(sessao.Token);

        var resposta = await logado.GetAsync("/api/candidatos/meu");

        // 404 (ainda não tem currículo) prova que passou pela autenticação: sem token seria 401.
        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task RotaProtegida_SemToken_Retorna401()
    {
        var resposta = await _cliente.GetAsync("/api/candidatos/meu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_Adulterado_Retorna401()
    {
        var sessao = await ApoioApi.RegistrarAsync(_cliente);
        var partes = sessao.Token.Split('.');
        // Troca o primeiro caractere da assinatura (6 bits inteiros, então os bytes mudam de fato).
        var assinatura = (partes[2][0] == 'A' ? 'B' : 'A') + partes[2][1..];
        var adulterado = $"{partes[0]}.{partes[1]}.{assinatura}";

        var resposta = await factory.CriarClienteApi(adulterado).GetAsync("/api/candidatos/meu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_Expirado_Retorna401()
    {
        var gerador = factory.Services.GetRequiredService<GeradorDeToken>();
        var usuario = new Usuario { Id = 1, Email = "expirado@exemplo.com" };
        // Gerado há 5 horas, com validade de 2: venceu há 3 horas (a tolerância é de 1 minuto).
        var (token, _) = gerador.Gerar(usuario, DateTime.UtcNow.AddHours(-5));

        var resposta = await factory.CriarClienteApi(token).GetAsync("/api/candidatos/meu");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private static string? Titulo(string json) =>
        System.Text.Json.JsonDocument.Parse(json).RootElement.GetProperty("title").GetString();
}
