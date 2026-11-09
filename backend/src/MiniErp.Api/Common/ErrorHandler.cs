using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Domain;

namespace MiniErp.Api.Common;

public class ErrorHandler(ILogger<ErrorHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, title, detail) = exception switch
        {
            DomainException e => (StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", e.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflito",
                "O registro foi alterado por outro usuário. Recarregue e tente novamente."),
            BadHttpRequestException e => (StatusCodes.Status400BadRequest, "Requisição inválida", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno", "Ocorreu um erro inesperado.")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Erro não tratado em {Path}", http.Request.Path);

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}
