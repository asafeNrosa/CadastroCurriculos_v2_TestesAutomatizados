using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Cadastro_Curriculos.Infrastructure;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (status, titulo) = exception switch
        {
            SqlException => (StatusCodes.Status503ServiceUnavailable,
                "Não foi possível acessar o banco de dados. Tente novamente em instantes."),
            _ => (StatusCodes.Status500InternalServerError,
                "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = titulo }
        });
    }
}
