using System.ComponentModel.DataAnnotations;

namespace Cadastro_Curriculos.Dtos;

public class ExperienciaRequest : IValidatableObject
{
    [Required(ErrorMessage = "A empresa é obrigatória.")]
    [MaxLength(150, ErrorMessage = "A empresa deve ter no máximo 150 caracteres.")]
    public string Empresa { get; set; } = string.Empty;

    [Required(ErrorMessage = "O cargo é obrigatório.")]
    [MaxLength(100, ErrorMessage = "O cargo deve ter no máximo 100 caracteres.")]
    public string Cargo { get; set; } = string.Empty;

    // Nullable de propósito: se o campo não vier no JSON, o [Required] acusa o erro
    // (um DateOnly não nulo viraria 01/01/0001 em silêncio).
    [Required(ErrorMessage = "A data de início é obrigatória.")]
    public DateOnly? DataInicio { get; set; }

    // Nulo = emprego atual.
    public DateOnly? DataFim { get; set; }

    [MaxLength(1000, ErrorMessage = "A descrição deve ter no máximo 1000 caracteres.")]
    public string? Descricao { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DataInicio is not null && DataFim is not null && DataFim < DataInicio)
        {
            yield return new ValidationResult(
                "A data de término não pode ser anterior à data de início.",
                [nameof(DataFim)]);
        }
    }
}
