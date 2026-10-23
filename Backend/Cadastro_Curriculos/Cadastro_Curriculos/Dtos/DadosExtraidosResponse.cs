namespace Cadastro_Curriculos.Dtos;

public record DadosExtraidosResponse(
    string? NomeCompleto, 
    string? Email, 
    string? Telefone, 
    string? AreaInteresse, 
    string? ResumoProfissional
       );