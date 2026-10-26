using System.Data;
using System.Data.Common;
using Pricely.Common.Application.Clock;
using Pricely.Common.Infrastructure.Data;
using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Common.Infrastructure.Outbox;
using Pricely.Common.Infrastructure.Serialization;
using Pricely.Modules.Catalog.Infrastructure.Database;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Pricely.Modules.Catalog.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class ProcessOutboxJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<ProcessOutboxJob> logger) : IJob
{
    private const string ModuleName = "Catalog";

    public async Task Execute(IJobExecutionContext context)
    {

        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        IReadOnlyList<OutboxMessageResponse> outboxMessages = await GetOutboxMessagesAsync(connection, transaction);

        if (outboxMessages.Count > 0)
        {
            logger.LogInformation("{Module} - {Count} outbox messages to process",
                ModuleName,
                outboxMessages.Count);
        }

        foreach (OutboxMessageResponse outboxMessage in outboxMessages)
        {
            Exception? exception = null;
            logger.LogInformation("{Module} - Begin process outbox message {Content}", ModuleName, outboxMessage.Content);
            try
            {
                IDomainEvent domainEvent = JsonConvert.DeserializeObject<IDomainEvent>(
                    outboxMessage.Content,
                    SerializerSettings.Instance)!;

                using IServiceScope scope = serviceScopeFactory.CreateScope();

                IEnumerable<IDomainEventHandler> handlers = DomainEventHandlersFactory.GetHandlers(
                    domainEvent.GetType(),
                    scope.ServiceProvider,
                    Application.AssemblyReference.Assembly);

                foreach (IDomainEventHandler domainEventHandler in handlers)
                {

                    logger.LogInformation("{Module} - Begin handle domain event {DomainEvent}", ModuleName, domainEvent.GetType());

                    await domainEventHandler.Handle(domainEvent, context.CancellationToken);

                    logger.LogInformation("{Module} - Completed handle domain event {DomainEvent}", ModuleName, domainEvent.GetType());
                }
            }
            catch (Exception caughtException)
            {
                logger.LogError(
                    caughtException,
                    "{Module} - Exception while processing outbox message {MessageId}",
                    ModuleName,
                    outboxMessage.Id);

                exception = caughtException;
            }

            await UpdateOutboxMessageAsync(connection, transaction, outboxMessage, exception);
        }

        await transaction.CommitAsync();

        if (outboxMessages.Count > 0)
        {
            logger.LogInformation("{Module} - Completed processing outbox messages", ModuleName);
        }
    }

    private async Task<IReadOnlyList<OutboxMessageResponse>> GetOutboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction)
    {

        string sql = $"""
                SELECT
                   id AS  {nameof(OutboxMessageResponse.Id)},
                   content AS {nameof(OutboxMessageResponse.Content)}
                FROM {Schemas.Catalog}.outbox_messages
                WHERE processed_on_utc IS NULL
                ORDER BY occurred_on_utc
                LIMIT {outboxOptions.Value.BatchSize}
                FOR UPDATE SKIP LOCKED
                """;


        IEnumerable<OutboxMessageResponse> outboxMessages = await connection.QueryAsync<OutboxMessageResponse>(
            sql,
            transaction: transaction);

        return outboxMessages.ToList();
    }

    private async Task UpdateOutboxMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        OutboxMessageResponse outboxMessage,
        Exception? exception)
    {
        const string sql =
            $"""
            UPDATE {Schemas.Catalog}.outbox_messages
            SET processed_on_utc = @ProcessedOnUtc,
                error = @Error
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                outboxMessage.Id,
                ProcessedOnUtc = dateTimeProvider.UtcNow,
                Error = exception?.ToString()
            },
            transaction: transaction);
    }

    internal sealed record OutboxMessageResponse(Guid Id, string Content);
}
