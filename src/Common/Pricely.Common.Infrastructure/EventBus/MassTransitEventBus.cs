using MassTransit;
using Pricely.Common.Application.EventBus;

namespace Pricely.Common.Infrastructure.EventBus;

/// <summary>
/// Publishes integration events on the MassTransit bus (in-memory). Consumers store them in their module inbox.
/// </summary>
internal sealed class MassTransitEventBus(IPublishEndpoint publishEndpoint) : IEventBus
{
    public Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}
