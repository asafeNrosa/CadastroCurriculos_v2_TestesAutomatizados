using System.Net.Http.Json;
using Cadastro_Curriculos.Dtos;

namespace Cadastro_Curriculos.Tests.Integracao;

internal static class ApoioApi
{
    public const string SenhaPadrao = "senha-segura-123";

    // E-mail único por chamada: os testes de uma classe compartilham o mesmo banco.
    public static string EmailUnico(string prefixo = "pessoa") => $"{prefixo}-{Guid.NewGuid():N}@exemplo.com";

    public static async Task<SessaoResponse> RegistrarAsync(
        HttpClient cliente, string? email = null, string senha = SenhaPadrao)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/auth/registrar",
            new RegistrarRequest { Email = email ?? EmailUnico("conta"), Senha = senha });

        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<SessaoResponse>())!;
    }

    public static CandidatoRequest NovoCandidato(string? email = null, int experiencias = 0) => new()
    {
        NomeCompleto = "Maria Eduarda Ferreira",
        Email = email ?? EmailUnico("candidato"),
        Telefone = "(41) 99999-0000",
        AreaInteresse = "Desenvolvimento backend",
        ResumoProfissional = "Estudante de ADS com foco em .NET.",
        Experiencias = Enumerable.Range(1, experiencias)
            .Select(i => new ExperienciaRequest
            {
                Empresa = $"Empresa {i}",
                Cargo = $"Cargo {i}",
                DataInicio = new DateOnly(2020, 1, 1).AddYears(i),
                DataFim = i == experiencias ? null : new DateOnly(2020, 12, 31).AddYears(i),
                Descricao = $"Atividades da experiência {i}."
            })
            .ToList()
    };

    public static async Task<CandidatoDetalheResponse> CadastrarAsync(HttpClient cliente, CandidatoRequest request)
    {
        var resposta = await cliente.PostAsJsonAsync("/api/candidatos", request);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<CandidatoDetalheResponse>())!;
    }

    public static async Task<CandidatoDetalheResponse> ObterAsync(HttpClient cliente, int id)
    {
        var detalhe = await cliente.GetFromJsonAsync<CandidatoDetalheResponse>($"/api/candidatos/{id}");
        return detalhe!;
    }
}
