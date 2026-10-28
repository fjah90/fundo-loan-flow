using System.Text.Json;
using LoanFlow.Application;

namespace LoanFlow.Infrastructure;

public sealed class OutboxEventPublisher(LoanFlowDbContext db) : IIntegrationEventPublisher
{
    public void Publish(CustomerSyncRequested integrationEvent) =>
        db.OutboxMessages.Add(new OutboxMessage
        {
            Type = nameof(CustomerSyncRequested),
            Payload = JsonSerializer.Serialize(integrationEvent, OutboxJson.Options),
            OccurredAt = DateTimeOffset.UtcNow,
        });
}
