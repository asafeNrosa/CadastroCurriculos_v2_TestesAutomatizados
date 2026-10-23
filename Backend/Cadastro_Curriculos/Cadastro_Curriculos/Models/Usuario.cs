using System.ComponentModel.DataAnnotations;

namespace CadastroCurriculos.Api.Models;

/// <summary>
/// Conta de acesso. É opcional: qualquer pessoa pode cadastrar um currículo sem criar conta,
/// mas só quem tem conta consegue editar ou excluir o próprio cadastro.
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    // Nunca guardamos a senha: apenas o hash gerado pelo PasswordHasher.
    [Required, MaxLength(500)]
    public string SenhaHash { get; set; } = string.Empty;

    public DateTime DataCadastro { get; set; }

    // Relacionamento 1:0..1 — cada usuário tem no máximo um currículo.
    public Candidato? Candidato { get; set; }
}
