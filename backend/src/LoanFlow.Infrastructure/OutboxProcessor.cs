using System.Text.Json;
using LoanFlow.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoanFlow.Infrastructure;

/// <summary>Delivers pending outbox rows in Id order, at least once; stops the batch at the first failure.</summary>
public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger, TimeSpan pollInterval)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(pollInterval);
        try
        {
            do
            {
                try
                {
                    await ProcessPendingAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Outbox poll failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LoanFlowDbContext>();
        var handler = scope.ServiceProvider.GetRequiredService<SyncCustomerWithExternalService>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.Id)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            try
            {
                var e = JsonSerializer.Deserialize<CustomerSyncRequested>(message.Payload, OutboxJson.Options)
                    ?? throw new InvalidOperationException("Outbox payload is empty.");
                await handler.HandleAsync(e, ct);
                message.ProcessedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException && ct.IsCancellationRequested))
            {
                message.Attempts++;
                message.LastError = ex.Message;
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Outbox message {Id} failed (attempt {Attempts})", message.Id, message.Attempts);
                break;
            }
        }
    }
}
