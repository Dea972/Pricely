using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Bogus;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Abstractions.Identity;
using Pricely.Modules.Users.Domain.Users;
using Pricely.Modules.Users.Infrastructure.Database;
using Pricely.Modules.Users.Infrastructure.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pricely.Modules.Users.IntegrationTests.Abstractions;

[Collection(nameof(IntegrationTestCollection))]
public abstract class BaseIntegrationTest : IDisposable
{
    protected static readonly Faker Faker = new();
    private readonly IServiceScope _scope;
    protected readonly ISender Sender;
    protected readonly UsersDbContext DbContext;
    protected readonly HttpClient HttpClient;
    protected readonly IIdentityProviderService IdentityProviderService;
    protected readonly IServiceProvider ServiceProvider;

    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        HttpClient = factory.CreateClient();
        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        ServiceProvider = _scope.ServiceProvider.GetRequiredService<IServiceProvider>();
        IdentityProviderService = _scope.ServiceProvider.GetRequiredService<IIdentityProviderService>();
    }

    protected async Task CleanDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM users.inbox_message_consumers;
            DELETE FROM users.inbox_messages;
            DELETE FROM users.outbox_message_consumers;
            DELETE FROM users.outbox_messages;
            DELETE FROM users.user_permissions;
            DELETE FROM users.user_roles;
            DELETE FROM users.users;
            """);
    }
    protected async Task<string> GetAdminAccessTokenAsync()
    {
        User adminUser = await DbContext.CreateAdminUserAsync(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            $"{Faker.Name.FirstName()}.{Faker.Name.LastName()}".ToLower(CultureInfo.InvariantCulture));

        Result<string> result = await IdentityProviderService.GenerateAccessTokenAsync(adminUser.IdentityId);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to generate access token for user '{adminUser.Email}': {result.Error}");
        }

        return result.Value;
    }

    protected async Task<string> GetAccessTokenAsync(string email, string userName)
    {
        Result<string> result =  await IdentityProviderService.GenerateAccessTokenAsync(userName);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to generate access token for user '{email}': {result.Error}");
        }

        return result.Value;
    }

    //internal sealed class AuthToken
    //{
    //    [JsonPropertyName("access_token")]
    //    public string AccessToken { get; init; }
    //}

    public void Dispose()
    {
        _scope.Dispose();
    }
}
