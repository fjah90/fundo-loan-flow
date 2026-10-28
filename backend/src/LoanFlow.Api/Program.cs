using LoanFlow.Application;
using LoanFlow.Domain;
using LoanFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LoanFlowDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("LoanFlow")));
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

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<LoanFlowDbContext>().Database.EnsureCreatedAsync();
}

app.Run();
