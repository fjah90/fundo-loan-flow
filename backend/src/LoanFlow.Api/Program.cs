using System.Text.Json;
using LoanFlow.Api;
using LoanFlow.Application;
using LoanFlow.Domain;
using LoanFlow.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoanFlowDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("LoanFlow"))
        // A duplicate-SSN race is an expected 409, not an error log (AR-44).
        .ConfigureWarnings(w => w.Log(
            (CoreEventId.SaveChangesFailed, LogLevel.Information),
            (RelationalEventId.CommandError, LogLevel.Information))));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LoanFlowDbContext>());
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();
var blacklistedSsns = builder.Configuration.GetSection("Blacklist:Ssns").Get<string[]>();
if (blacklistedSsns is not { Length: > 0 })
    throw new InvalidOperationException("Blacklist:Ssns is required.");
builder.Services.AddSingleton<ISsnBlacklist>(new ConfigurationSsnBlacklist(blacklistedSsns));
builder.Services.AddSingleton<IDenyRule, NewYorkStateRule>();
builder.Services.AddSingleton<IDenyRule, BlacklistedSsnRule>();
builder.Services.AddScoped<RuleEngine>();
builder.Services.AddScoped<SubmitLoanApplication>();
builder.Services.AddScoped<SyncCustomerWithExternalService>();
builder.Services.AddHttpClient<IExternalCustomerService, ExternalCustomerHttpClient>(c =>
{
    c.BaseAddress = new Uri(builder.Configuration["ExternalService:BaseUrl"] ?? "http://localhost:4000/");
    c.Timeout = TimeSpan.FromSeconds(10);
});
if (builder.Configuration.GetValue("Outbox:Enabled", true))
    builder.Services.AddHostedService(sp => new OutboxProcessor(
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<ILogger<OutboxProcessor>>(),
        TimeSpan.FromSeconds(builder.Configuration.GetValue("Outbox:PollIntervalSeconds", 2))));

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
