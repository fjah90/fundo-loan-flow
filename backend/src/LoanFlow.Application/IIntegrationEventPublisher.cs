namespace LoanFlow.Application;

public interface IIntegrationEventPublisher
{
    /// <summary>Stages the event; it becomes durable with the next commit.</summary>
    void Publish(CustomerSyncRequested integrationEvent);
}
