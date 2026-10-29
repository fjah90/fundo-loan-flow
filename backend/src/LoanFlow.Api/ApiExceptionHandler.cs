using LoanFlow.Application;
using LoanFlow.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LoanFlow.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> log)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        ProblemDetails problem;
        switch (exception)
        {
            case DomainValidationException ex:
                problem = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [ex.Field] = [ex.Message],
                })
                { Status = StatusCodes.Status400BadRequest, Title = "One or more validation errors occurred." };
                break;
            case CustomerConflictException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Another request changed this customer. Try again.",
                };
                break;
            default:
                log.LogError(exception, "Unhandled exception");
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                };
                break;
        }

        http.Response.StatusCode = problem.Status!.Value;
        await problems.WriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem });
        return true;
    }
}
