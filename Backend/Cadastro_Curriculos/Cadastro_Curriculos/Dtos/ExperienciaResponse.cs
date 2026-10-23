namespace Cadastro_Curriculos.Dtos;

public record ExperienciaResponse(
    int Id,
    string Empresa,
    string Cargo,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string? Descricao);
