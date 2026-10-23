using Cadastro_Curriculos.Dtos;
using Cadastro_Curriculos.Infrastructure;
using CadastroCurriculos.Api.Data;
using CadastroCurriculos.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cadastro_Curriculos.Controllers;

[ApiController]
[Route("api/candidatos")]
public class CandidatosController(AppDbContext context) : ControllerBase
{
    // GET api/candidatos
    [HttpGet]
    public async Task<ActionResult<List<CandidatoResponse>>> Listar()
    {
        var candidatos = await context.Candidatos
            .AsNoTracking()
            .OrderByDescending(c => c.DataCadastro)
            .Select(c => ParaResponse(c))
            .ToListAsync();

        return Ok(candidatos);
    }

    // GET api/candidatos/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CandidatoDetalheResponse>> ObterPorId(int id)
    {
        var candidato = await context.Candidatos
            .AsNoTracking()
            .Include(c => c.Experiencias)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidato is null)
            return NaoEncontrado();

        return Ok(ParaDetalhe(candidato));
    }

    // GET api/candidatos/meu  (currículo do usuário logado)
    [Authorize]
    [HttpGet("meu")]
    public async Task<ActionResult<CandidatoDetalheResponse>> ObterMeu()
    {
        var usuarioId = UsuarioLogadoId();

        var candidato = await context.Candidatos
            .AsNoTracking()
            .Include(c => c.Experiencias)
            .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId);

        if (candidato is null)
            return NaoEncontrado("Você ainda não cadastrou um currículo.");

        return Ok(ParaDetalhe(candidato));
    }

    // POST api/candidatos  (aberto a todos; se houver token válido, o cadastro passa a ser do usuário)
    [HttpPost]
    public async Task<ActionResult<CandidatoDetalheResponse>> Cadastrar(CandidatoRequest request)
    {
        var usuarioId = UsuarioLogadoId();

        if (usuarioId is not null && await context.Candidatos.AnyAsync(c => c.UsuarioId == usuarioId))
        {
            return Problem(
                title: "Você já possui um currículo cadastrado. Edite o existente.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var candidato = new Candidato
        {
            UsuarioId = usuarioId,
            DataCadastro = DateTime.UtcNow
        };
        Preencher(candidato, request);

        context.Candidatos.Add(candidato);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ErrosDeBanco.ViolacaoDeUnicidade(ex))
        {
            return EmailJaCadastrado();
        }

        return CreatedAtAction(nameof(ObterPorId), new { id = candidato.Id }, ParaDetalhe(candidato));
    }

    // PUT api/candidatos/5  (somente o dono)
    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CandidatoDetalheResponse>> Atualizar(int id, CandidatoRequest request)
    {
        var candidato = await context.Candidatos
            .Include(c => c.Experiencias)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidato is null)
            return NaoEncontrado();

        if (!candidato.PertenceA(UsuarioLogadoId()))
            return Proibido("Você só pode editar o seu próprio cadastro.");

        Preencher(candidato, request);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ErrosDeBanco.ViolacaoDeUnicidade(ex))
        {
            return EmailJaCadastrado();
        }

        return Ok(ParaDetalhe(candidato));
    }

    // DELETE api/candidatos/5  (somente o dono)
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var candidato = await context.Candidatos
            .Include(c => c.Experiencias)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (candidato is null)
            return NaoEncontrado();

        if (!candidato.PertenceA(UsuarioLogadoId()))
            return Proibido("Você só pode excluir o seu próprio cadastro.");

        context.Candidatos.Remove(candidato);
        await context.SaveChangesAsync();

        return NoContent();
    }

    // Id do usuário vem do claim "sub" do token (MapInboundClaims está desligado no Program.cs).
    private int? UsuarioLogadoId() =>
        int.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;

    private ObjectResult NaoEncontrado(string titulo = "Candidato não encontrado.") =>
        Problem(title: titulo, statusCode: StatusCodes.Status404NotFound);

    private ObjectResult Proibido(string titulo) =>
        Problem(title: titulo, statusCode: StatusCodes.Status403Forbidden);

    private ObjectResult EmailJaCadastrado() =>
        Problem(
            title: "Já existe um candidato cadastrado com este e-mail.",
            statusCode: StatusCodes.Status409Conflict);

    // Copia o request para a entidade. As experiências são substituídas por inteiro:
    // as antigas (se houver) são removidas e as enviadas pelo cliente viram as novas.
    private void Preencher(Candidato candidato, CandidatoRequest request)
    {
        candidato.NomeCompleto = request.NomeCompleto.Trim();
        candidato.Email = request.Email.Trim().ToLowerInvariant();
        candidato.Telefone = LimparOpcional(request.Telefone);
        candidato.AreaInteresse = LimparOpcional(request.AreaInteresse);
        candidato.ResumoProfissional = LimparOpcional(request.ResumoProfissional);

        context.ExperienciasProfissionais.RemoveRange(candidato.Experiencias.ToList());
        candidato.Experiencias.Clear();

        foreach (var experiencia in request.Experiencias.Where(e => e is not null))
        {
            candidato.Experiencias.Add(new ExperienciaProfissional
            {
                Empresa = experiencia.Empresa.Trim(),
                Cargo = experiencia.Cargo.Trim(),
                DataInicio = experiencia.DataInicio!.Value,
                DataFim = experiencia.DataFim,
                Descricao = LimparOpcional(experiencia.Descricao)
            });
        }
    }

    private static string? LimparOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static CandidatoResponse ParaResponse(Candidato c) =>
        new(c.Id, c.NomeCompleto, c.Email, c.Telefone, c.AreaInteresse, c.ResumoProfissional, DateTime.SpecifyKind(c.DataCadastro, DateTimeKind.Utc));

    private CandidatoDetalheResponse ParaDetalhe(Candidato c) =>
        new(c.Id,
            c.NomeCompleto,
            c.Email,
            c.Telefone,
            c.AreaInteresse,
            c.ResumoProfissional,
            DateTime.SpecifyKind(c.DataCadastro, DateTimeKind.Utc),
            c.Experiencias
                .OrderByDescending(e => e.DataInicio)
                .ThenByDescending(e => e.Id)
                .Select(e => new ExperienciaResponse(e.Id, e.Empresa, e.Cargo, e.DataInicio, e.DataFim, e.Descricao))
                .ToList(),
            c.PertenceA(UsuarioLogadoId()));
}
