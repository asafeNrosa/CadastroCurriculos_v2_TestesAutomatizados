using Cadastro_Curriculos.Dtos;
using Cadastro_Curriculos.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cadastro_Curriculos.Controllers;

[ApiController]
[Route("api/curriculos")]
public class CurriculosController(LeitorPdf leitorPdf, CurriculoExtrator extrator) : ControllerBase
{
    // Margem acima dos 5 MB para o cabeçalho multipart; acima disso o servidor recusa com 413.
    private const long LimiteRequisicaoBytes = 6 * 1024 * 1024;

    // POST api/curriculos/extrair
    [HttpPost("extrair")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteRequisicaoBytes)]
    public async Task<ActionResult<DadosExtraidosResponse>> Extrair(IFormFile? arquivo)
    {
        var erroValidacao = await leitorPdf.ValidarAsync(arquivo);
        if (erroValidacao is not null)
            return Problem(title: erroValidacao, statusCode: StatusCodes.Status400BadRequest);

        string texto;
        try
        {
            texto = await leitorPdf.ExtrairTextoAsync(arquivo!);
        }
        catch (FalhaLeituraPdfException ex)
        {
            return Problem(title: ex.Message, statusCode: StatusCodes.Status422UnprocessableEntity);
        }

        if (string.IsNullOrWhiteSpace(texto))
            return Problem(
                title: "Nenhum texto foi encontrado no PDF. Ele pode ser uma imagem digitalizada. Preencha os dados manualmente.",
                statusCode: StatusCodes.Status422UnprocessableEntity);

        return Ok(extrator.Extrair(texto));
    }
}