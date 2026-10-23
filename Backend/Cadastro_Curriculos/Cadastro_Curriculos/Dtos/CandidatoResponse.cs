namespace Cadastro_Curriculos.Dtos
{
    public record CandidatoResponse(
    int Id,
    string NomeCompleto,
    string Email,
    string? Telefone,
    string? AreaInteresse,
    string? ResumoProfissional,
    DateTime DataCadastro);
}
