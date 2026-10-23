namespace Cadastro_Curriculos.Dtos;

/// <summary>
/// Resposta completa de um candidato (detalhes, cadastro e edição).
/// A lista continua usando o CandidatoResponse, que é mais enxuto.
/// </summary>
public record CandidatoDetalheResponse(
    int Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaInteresse,
    string? ResumoProfissional,
    DateTime DataCadastro,
    IReadOnlyList<ExperienciaResponse> Experiencias,
    bool PodeEditar);
