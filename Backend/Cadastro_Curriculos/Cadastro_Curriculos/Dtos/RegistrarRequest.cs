using System.ComponentModel.DataAnnotations;

namespace Cadastro_Curriculos.Dtos;

public class RegistrarRequest
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [MaxLength(254, ErrorMessage = "O e-mail deve ter no máximo 254 caracteres.")]
    [RegularExpression(CandidatoRequest.EmailRegex, ErrorMessage = "Informe um e-mail válido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    [MaxLength(100, ErrorMessage = "A senha deve ter no máximo 100 caracteres.")]
    public string Senha { get; set; } = string.Empty;
}
