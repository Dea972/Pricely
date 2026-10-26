using System.Data;
using System.Data.Common;
using Pricely.Common.Application.Clock;
using Pricely.Common.Application.Data;
using Pricely.Common.Application.EventBus;
using Pricely.Common.Infrastructure.Inbox;
using Pricely.Common.Infrastructure.Serialization;
using Pricely.Modules.Users.Infrastructure.Database;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Quartz;

namespace Pricely.Modules.Users.Infrastructure.Inbox;


[DisallowConcurrentExecution]
internal sealed class ProcessInboxJob(
    IDbConnectionFactory dbConnectionFactory,
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<InboxOptions> inboxOptions,
    ILogger<ProcessInboxJob> logger) : IJob
{
    private const string ModuleName = "IO";

    public async Task Execute(IJobExecutionContext context)
    {
        await using DbConnection connection = await dbConnectionFactory.OpenConnectionAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        IReadOnlyList<InboxMessageResponse> inboxMessages = await GetInboxMessagesAsync(connection, transaction);
        if (inboxMessages.Count > 0)
        {
            logger.LogInformation("{Module} - {Count} inbox messages to process",
                ModuleName,
                inboxMessages.Count);
        }
        foreach (InboxMessageResponse inboxMessage in inboxMessages)
        {
            Exception? exception = null;

            logger.LogInformation("{Module} - Begin process inbox message {Content}", ModuleName, inboxMessage.Content);
            try
            {
                IIntegrationEvent integrationEvent = JsonConvert.DeserializeObject<IIntegrationEvent>(
                    inboxMessage.Content,
                    SerializerSettings.Instance)!;

                using IServiceScope scope = serviceScopeFactory.CreateScope();

                IEnumerable<IIntegrationEventHandler> handlers = IntegrationEventHandlersFactory.GetHandlers(
                    integrationEvent.GetType(),
                    scope.ServiceProvider,
                    Presentation.AssemblyReference.Assembly);

                foreach (IIntegrationEventHandler integrationEventHandler in handlers)
                {
                    logger.LogInformation("{Module} - Begin handle integration event {IntegrationEvent}", ModuleName, integrationEvent.GetType());

                    await integrationEventHandler.Handle(integrationEvent, context.CancellationToken);

                    logger.LogInformation("{Module} - Completed handle integration event {IntegrationEvent}", ModuleName, integrationEvent.GetType());
                }
            }
            catch (Exception caughtException)
            {
                logger.LogError(
                    caughtException,
                    "{Module} - Exception while processing inbox message {MessageId}",
                    ModuleName,
                    inboxMessage.Id);

                exception = caughtException;
            }

            await UpdateInboxMessageAsync(connection, transaction, inboxMessage, exception);
        }

        await transaction.CommitAsync();
        if (inboxMessages.Count > 0)
        {
            logger.LogInformation("{Module} - Completed processing inbox messages", ModuleName);
        }
    }

    private async Task<IReadOnlyList<InboxMessageResponse>> GetInboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction)
    {

         string sql = $"""
                SELECT
                   id AS  {nameof(InboxMessageResponse.Id)},
                   content AS {nameof(InboxMessageResponse.Content)}
                FROM {Schemas.Users}.inbox_messages
                WHERE processed_on_utc IS NULL
                ORDER BY occurred_on_utc
                LIMIT {inboxOptions.Value.BatchSize}
                FOR UPDATE SKIP LOCKED
                """;


        IEnumerable<InboxMessageResponse> inboxMessages = await connection.QueryAsync<InboxMessageResponse>(
            sql,
            transaction: transaction);

        return inboxMessages.AsList();
    }

    private async Task UpdateInboxMessageAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        InboxMessageResponse inboxMessage,
        Exception? exception)
    {
        const string sql =
            $"""
            UPDATE {Schemas.Users}.inbox_messages
            SET processed_on_utc = @ProcessedOnUtc,
                error = @Error
            WHERE id = @Id
            """;

        await connection.ExecuteAsync(
            sql,
            new
            {
                inboxMessage.Id,
                ProcessedOnUtc = dateTimeProvider.UtcNow,
                Error = exception?.ToString()
            },
            transaction: transaction);
    }

    internal sealed record InboxMessageResponse(Guid Id, string Content);
}
