using System.ComponentModel.DataAnnotations;

namespace CadastroCurriculos.Api.Models;

/// <summary>Uma experiência profissional de um candidato (lado "N" do relacionamento 1:N).</summary>
public class ExperienciaProfissional
{
    public int Id { get; set; }

    public int CandidatoId { get; set; }

    public Candidato? Candidato { get; set; }

    [Required, MaxLength(150)]
    public string Empresa { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Cargo { get; set; } = string.Empty;

    public DateOnly DataInicio { get; set; }

    // Nulo significa "emprego atual".
    public DateOnly? DataFim { get; set; }

    [MaxLength(1000)]
    public string? Descricao { get; set; }

    public bool EmAndamento => DataFim is null;
}
