using LoanFlow.Application;
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
            case ArgumentException { ParamName: not null } ex:
                problem = new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    [ex.ParamName] = [ex.Message.Split(" (Parameter '")[0]],
                })
                { Status = StatusCodes.Status400BadRequest, Title = "One or more validation errors occurred." };
                break;
            case DuplicateCustomerException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Customer already exists.",
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
