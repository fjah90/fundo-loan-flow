using LoanFlow.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace LoanFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDenyRule, NewYorkStateRule>();
        services.AddSingleton<IDenyRule, BlacklistedSsnRule>();
        services.AddScoped<RuleEngine>();
        services.AddScoped<SubmitLoanApplication>();
        services.AddScoped<SyncCustomerWithExternalService>();
        return services;
    }
}
