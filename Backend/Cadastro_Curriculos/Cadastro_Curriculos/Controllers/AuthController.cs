using Cadastro_Curriculos.Dtos;
using Cadastro_Curriculos.Infrastructure;
using Cadastro_Curriculos.Services;
using CadastroCurriculos.Api.Data;
using CadastroCurriculos.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadastro_Curriculos.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    AppDbContext context,
    IPasswordHasher<Usuario> hasher,
    GeradorDeToken geradorDeToken) : ControllerBase
{
    // POST api/auth/registrar
    [HttpPost("registrar")]
    public async Task<ActionResult<SessaoResponse>> Registrar(RegistrarRequest request)
    {
        var usuario = new Usuario
        {
            Email = NormalizarEmail(request.Email),
            DataCadastro = DateTime.UtcNow
        };
        usuario.SenhaHash = hasher.HashPassword(usuario, request.Senha);

        context.Usuarios.Add(usuario);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ErrosDeBanco.ViolacaoDeUnicidade(ex))
        {
            return Problem(
                title: "Já existe uma conta cadastrada com este e-mail.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return StatusCode(StatusCodes.Status201Created, CriarSessao(usuario));
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<SessaoResponse>> Login(LoginRequest request)
    {
        var email = NormalizarEmail(request.Email);
        var usuario = await context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        // Mesma mensagem para e-mail inexistente e senha errada: não revela quais e-mails têm conta.
        var resultado = usuario is null
            ? PasswordVerificationResult.Failed
            : hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.Senha);

        if (usuario is null || resultado == PasswordVerificationResult.Failed)
            return Problem(title: "E-mail ou senha inválidos.", statusCode: StatusCodes.Status401Unauthorized);

        return Ok(CriarSessao(usuario));
    }

    private SessaoResponse CriarSessao(Usuario usuario)
    {
        var (token, expiraEm) = geradorDeToken.Gerar(usuario);
        return new SessaoResponse(token, usuario.Email, expiraEm);
    }

    private static string NormalizarEmail(string email) => email.Trim().ToLowerInvariant();
}
