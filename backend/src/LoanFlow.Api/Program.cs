using System.Text.Json;
using LoanFlow.Api;
using LoanFlow.Application;
using LoanFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);

// Every automatic 400 goes through ProblemDetailsFactory with clean camelCase field keys (AR-48.2).
builder.Services.AddControllers()
    .AddJsonOptions(o => o.AllowInputFormatterExceptionMessages = false)
    .ConfigureApiBehaviorOptions(o =>
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var cleaned = new ModelStateDictionary();
            var entries = ctx.ModelState.Where(e => e.Value is { Errors.Count: > 0 }).ToList();
            var hasFieldErrors = entries.Any(e => e.Key != "request");
            foreach (var (key, entry) in entries)
            {
                if (hasFieldErrors && key == "request")
                    continue;
                var field = key == "$" ? "" : JsonNamingPolicy.CamelCase.ConvertName(key.StartsWith("$.") ? key[2..] : key);
                foreach (var error in entry!.Errors)
                    cleaned.AddModelError(field, string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage);
            }

            var problem = ctx.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(ctx.HttpContext, cleaned);
            return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
        });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();
app.UseExceptionHandler();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<LoanFlowDbContext>().Database.EnsureCreatedAsync();
}

app.MapControllers();

app.Run();

public partial class Program;
