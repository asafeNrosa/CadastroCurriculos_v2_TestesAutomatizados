using System.Net;
using System.Net.Http.Json;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests.Integracao;

public class CandidatosApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _anonimo = factory.CriarClienteApi();

    private async Task<HttpClient> NovoClienteLogadoAsync()
    {
        var sessao = await ApoioApi.RegistrarAsync(_anonimo);
        return factory.CriarClienteApi(sessao.Token);
    }

    // ---------- Cadastro (POST) ----------

    [Fact]
    public async Task Cadastrar_SemLogin_Retorna201ComLocationEPodeEditarFalso()
    {
        var request = ApoioApi.NovoCandidato();

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", request);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
        var criado = await resposta.Content.ReadFromJsonAsync<CandidatoDetalheResponse>();
        Assert.EndsWith($"/api/candidatos/{criado!.Id}", resposta.Headers.Location!.ToString());
        Assert.Equal(request.Email.ToLowerInvariant(), criado.Email);
        Assert.False(criado.PodeEditar);
        Assert.Empty(criado.Experiencias);
    }

    [Fact]
    public async Task Cadastrar_SemLogin_NaoVinculaAUsuario()
    {
        var criado = await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato());

        var usuarioId = await factory.ConsultarBancoAsync(
            db => Task.FromResult(db.Candidatos.Single(c => c.Id == criado.Id).UsuarioId));

        Assert.Null(usuarioId);
    }

    [Fact]
    public async Task Cadastrar_ComExperiencias_GravaTodasEDevolveDaMaisRecenteParaAMaisAntiga()
    {
        var criado = await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato(experiencias: 3));

        var detalhe = await ApoioApi.ObterAsync(_anonimo, criado.Id);

        Assert.Equal(3, detalhe.Experiencias.Count);
        Assert.Equal(new[] { "Empresa 3", "Empresa 2", "Empresa 1" }, detalhe.Experiencias.Select(e => e.Empresa).ToArray());
        Assert.Null(detalhe.Experiencias[0].DataFim);          // a mais recente é o emprego atual
        Assert.NotNull(detalhe.Experiencias[1].DataFim);
    }

    [Fact]
    public async Task Cadastrar_EmailDuplicado_Retorna409()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato(email));

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", ApoioApi.NovoCandidato(email));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_EmailDuplicadoComOutraCaixa_Retorna409()
    {
        var email = ApoioApi.EmailUnico();
        await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato(email));

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos",
            ApoioApi.NovoCandidato(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_NomeVazio_Retorna400()
    {
        var request = ApoioApi.NovoCandidato();
        request.NomeCompleto = "";

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", request);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_ExperienciaSemEmpresa_Retorna400IndicandoOCampoAninhado()
    {
        var request = ApoioApi.NovoCandidato(experiencias: 1);
        request.Experiencias[0].Empresa = "";

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", request);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        var corpo = await resposta.Content.ReadAsStringAsync();
        Assert.Contains("Experiencias[0].Empresa", corpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cadastrar_ExperienciaComTerminoAntesDoInicio_Retorna400()
    {
        var request = ApoioApi.NovoCandidato(experiencias: 1);
        request.Experiencias[0].DataInicio = new DateOnly(2024, 5, 1);
        request.Experiencias[0].DataFim = new DateOnly(2024, 1, 1);

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", request);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_MaisExperienciasQueOLimite_Retorna400()
    {
        var request = ApoioApi.NovoCandidato(experiencias: CandidatoRequest.MaximoDeExperiencias + 1);

        var resposta = await _anonimo.PostAsJsonAsync("/api/candidatos", request);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastrar_Logado_VinculaAoUsuarioEPodeEditarVerdadeiro()
    {
        var logado = await NovoClienteLogadoAsync();

        var criado = await ApoioApi.CadastrarAsync(logado, ApoioApi.NovoCandidato());

        Assert.True(criado.PodeEditar);
        var meu = await logado.GetFromJsonAsync<CandidatoDetalheResponse>("/api/candidatos/meu");
        Assert.Equal(criado.Id, meu!.Id);
    }

    [Fact]
    public async Task Cadastrar_LogadoQueJaTemCurriculo_Retorna409()
    {
        var logado = await NovoClienteLogadoAsync();
        await ApoioApi.CadastrarAsync(logado, ApoioApi.NovoCandidato());

        var resposta = await logado.PostAsJsonAsync("/api/candidatos", ApoioApi.NovoCandidato());

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    // ---------- Consulta (GET) ----------

    [Fact]
    public async Task Listar_IncluiOCadastroFeito()
    {
        var criado = await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato());

        var lista = await _anonimo.GetFromJsonAsync<List<CandidatoResponse>>("/api/candidatos");

        Assert.Contains(lista!, c => c.Id == criado.Id);
    }

    [Fact]
    public async Task ObterPorId_Inexistente_Retorna404()
    {
        var resposta = await _anonimo.GetAsync("/api/candidatos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task PodeEditar_DependeDeQuemConsulta()
    {
        var dono = await NovoClienteLogadoAsync();
        var outro = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        Assert.True((await ApoioApi.ObterAsync(dono, criado.Id)).PodeEditar);
        Assert.False((await ApoioApi.ObterAsync(outro, criado.Id)).PodeEditar);
        Assert.False((await ApoioApi.ObterAsync(_anonimo, criado.Id)).PodeEditar);
    }

    [Fact]
    public async Task ObterMeu_LogadoSemCurriculo_Retorna404()
    {
        var logado = await NovoClienteLogadoAsync();

        var resposta = await logado.GetAsync("/api/candidatos/meu");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    // ---------- Edição (PUT) ----------

    [Fact]
    public async Task Atualizar_PeloDono_Retorna200ESubstituiOsDadosEAsExperiencias()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato(experiencias: 2));

        var novo = ApoioApi.NovoCandidato(experiencias: 1);
        novo.NomeCompleto = "Nome Atualizado";
        novo.Experiencias[0].Empresa = "Empresa Nova";

        var resposta = await dono.PutAsJsonAsync($"/api/candidatos/{criado.Id}", novo);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var detalhe = await ApoioApi.ObterAsync(dono, criado.Id);
        Assert.Equal("Nome Atualizado", detalhe.NomeCompleto);
        var unica = Assert.Single(detalhe.Experiencias);
        Assert.Equal("Empresa Nova", unica.Empresa);

        // As experiências antigas foram removidas do banco, não ficaram órfãs.
        var total = await factory.ConsultarBancoAsync(
            db => Task.FromResult(db.ExperienciasProfissionais.Count(e => e.CandidatoId == criado.Id)));
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task Atualizar_RemovendoTodasAsExperiencias_DeixaAListaVazia()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato(experiencias: 2));

        var resposta = await dono.PutAsJsonAsync($"/api/candidatos/{criado.Id}", ApoioApi.NovoCandidato(experiencias: 0));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Empty((await ApoioApi.ObterAsync(dono, criado.Id)).Experiencias);
    }

    [Fact]
    public async Task Atualizar_MantendoOProprioEmail_Retorna200()
    {
        var dono = await NovoClienteLogadoAsync();
        var email = ApoioApi.EmailUnico();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato(email));

        var resposta = await dono.PutAsJsonAsync($"/api/candidatos/{criado.Id}", ApoioApi.NovoCandidato(email));

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_ParaEmailDeOutroCandidato_Retorna409()
    {
        var emailOcupado = ApoioApi.EmailUnico();
        await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato(emailOcupado));
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        var resposta = await dono.PutAsJsonAsync($"/api/candidatos/{criado.Id}", ApoioApi.NovoCandidato(emailOcupado));

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_SemToken_Retorna401()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        var resposta = await _anonimo.PutAsJsonAsync($"/api/candidatos/{criado.Id}", ApoioApi.NovoCandidato());

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_CadastroDeOutroUsuario_Retorna403ENaoAlteraNada()
    {
        var dono = await NovoClienteLogadoAsync();
        var intruso = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        var invasao = ApoioApi.NovoCandidato();
        invasao.NomeCompleto = "Invasor";
        var resposta = await intruso.PutAsJsonAsync($"/api/candidatos/{criado.Id}", invasao);

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal(criado.NomeCompleto, (await ApoioApi.ObterAsync(dono, criado.Id)).NomeCompleto);
    }

    [Fact]
    public async Task Atualizar_CadastroAnonimo_Retorna403MesmoParaUsuarioLogado()
    {
        var criado = await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato());
        var logado = await NovoClienteLogadoAsync();

        var resposta = await logado.PutAsJsonAsync($"/api/candidatos/{criado.Id}", ApoioApi.NovoCandidato());

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_Inexistente_Retorna404()
    {
        var logado = await NovoClienteLogadoAsync();

        var resposta = await logado.PutAsJsonAsync("/api/candidatos/999999", ApoioApi.NovoCandidato());

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Atualizar_ComDadosInvalidos_Retorna400()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());
        var invalido = ApoioApi.NovoCandidato();
        invalido.Email = "isto-nao-e-email";

        var resposta = await dono.PutAsJsonAsync($"/api/candidatos/{criado.Id}", invalido);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    // ---------- Exclusão (DELETE) ----------

    [Fact]
    public async Task Excluir_PeloDono_Retorna204ERemoveOCadastroEAsExperiencias()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato(experiencias: 2));

        var resposta = await dono.DeleteAsync($"/api/candidatos/{criado.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _anonimo.GetAsync($"/api/candidatos/{criado.Id}")).StatusCode);

        // Cascade: as experiências somem junto com o candidato.
        var restantes = await factory.ConsultarBancoAsync(
            db => Task.FromResult(db.ExperienciasProfissionais.Count(e => e.CandidatoId == criado.Id)));
        Assert.Equal(0, restantes);
    }

    [Fact]
    public async Task Excluir_PeloDono_PermiteCadastrarUmNovoCurriculo()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());
        await dono.DeleteAsync($"/api/candidatos/{criado.Id}");

        var resposta = await dono.PostAsJsonAsync("/api/candidatos", ApoioApi.NovoCandidato());

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_SemToken_Retorna401()
    {
        var dono = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        var resposta = await _anonimo.DeleteAsync($"/api/candidatos/{criado.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_CadastroDeOutroUsuario_Retorna403ENaoRemove()
    {
        var dono = await NovoClienteLogadoAsync();
        var intruso = await NovoClienteLogadoAsync();
        var criado = await ApoioApi.CadastrarAsync(dono, ApoioApi.NovoCandidato());

        var resposta = await intruso.DeleteAsync($"/api/candidatos/{criado.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _anonimo.GetAsync($"/api/candidatos/{criado.Id}")).StatusCode);
    }

    [Fact]
    public async Task Excluir_CadastroAnonimo_Retorna403()
    {
        var criado = await ApoioApi.CadastrarAsync(_anonimo, ApoioApi.NovoCandidato());
        var logado = await NovoClienteLogadoAsync();

        var resposta = await logado.DeleteAsync($"/api/candidatos/{criado.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);
    }

    [Fact]
    public async Task Excluir_Inexistente_Retorna404()
    {
        var logado = await NovoClienteLogadoAsync();

        var resposta = await logado.DeleteAsync("/api/candidatos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }
}
