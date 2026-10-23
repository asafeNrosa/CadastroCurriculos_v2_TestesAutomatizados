using System.ComponentModel.DataAnnotations;

namespace CadastroCurriculos.Api.Models;

public class Candidato
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(100)]
    public string? AreaInteresse { get; set; }

    [MaxLength(2000)]
    public string? ResumoProfissional { get; set; }

    public DateTime DataCadastro { get; set; }

    // Dono do currículo. Nulo = cadastro anônimo (ninguém consegue editar ou excluir).
    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public List<ExperienciaProfissional> Experiencias { get; set; } = [];

    /// <summary>Regra de negócio: só o dono (usuário logado) pode alterar ou excluir o cadastro.</summary>
    public bool PertenceA(int? usuarioId) => UsuarioId is not null && UsuarioId == usuarioId;
}
