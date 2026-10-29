using LoanFlow.Application;
using LoanFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace LoanFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LoanFlowDbContext>(o =>
            o.UseNpgsql(new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("LoanFlow"))
                {
                    // No Kerberos here; avoids the libgssapi probe on the chiseled image (AR-66).
                    GssEncryptionMode = GssEncryptionMode.Disable,
                }.ConnectionString)
                // A duplicate-SSN race is an expected 409, not an error log (AR-44).
                .ConfigureWarnings(w => w.Log(
                    (CoreEventId.SaveChangesFailed, LogLevel.Information),
                    (RelationalEventId.CommandError, LogLevel.Information))));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<LoanFlowDbContext>());
        services.AddScoped<ICustomerRepository, EfCustomerRepository>();
        services.AddScoped<IIntegrationEventPublisher, OutboxEventPublisher>();

        var blacklistedSsns = configuration.GetSection("Blacklist:Ssns").Get<string[]>();
        if (blacklistedSsns is not { Length: > 0 })
            throw new InvalidOperationException("Blacklist:Ssns is required.");
        services.AddSingleton<ISsnBlacklist>(new ConfigurationSsnBlacklist(blacklistedSsns));

        var externalServiceBaseUrl = configuration["ExternalService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(externalServiceBaseUrl))
            throw new InvalidOperationException("ExternalService:BaseUrl is required.");
        services.AddHttpClient<IExternalCustomerService, ExternalCustomerHttpClient>(c =>
        {
            c.BaseAddress = new Uri(externalServiceBaseUrl);
            c.Timeout = TimeSpan.FromSeconds(10);
        });

        if (configuration.GetValue("Outbox:Enabled", true))
            services.AddHostedService(sp => new OutboxProcessor(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<OutboxProcessor>>(),
                TimeSpan.FromSeconds(configuration.GetValue("Outbox:PollIntervalSeconds", 2))));
        return services;
    }
}
