using System.ComponentModel.DataAnnotations;

namespace Cadastro_Curriculos.Dtos
{
    public class CandidatoRequest : IValidatableObject
    {
        public const string EmailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        public const int MaximoDeExperiencias = 10;

        [Required(ErrorMessage = "O nome completo é obrigatório.")]
        [MaxLength(150, ErrorMessage = "O nome completo deve ter no máximo 150 caracteres.")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [MaxLength(254, ErrorMessage = "O e-mail deve ter no máximo 254 caracteres.")]
        [RegularExpression(EmailRegex, ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20, ErrorMessage = "O telefone deve ter no máximo 20 caracteres.")]
        public string? Telefone { get; set; }

        [MaxLength(100, ErrorMessage = "A área de interesse deve ter no máximo 100 caracteres.")]
        public string? AreaInteresse { get; set; }

        [MaxLength(2000, ErrorMessage = "O resumo profissional deve ter no máximo 2000 caracteres.")]
        public string? ResumoProfissional { get; set; }

        // Lista opcional: quem não envia o campo cadastra sem experiências.
        public List<ExperienciaRequest> Experiencias { get; set; } = [];

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Experiencias is { Count: > MaximoDeExperiencias })
            {
                yield return new ValidationResult(
                    $"Informe no máximo {MaximoDeExperiencias} experiências profissionais.",
                    [nameof(Experiencias)]);
            }
        }
    }
}
