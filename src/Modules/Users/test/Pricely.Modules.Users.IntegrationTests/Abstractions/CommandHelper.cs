using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bogus;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Application.Users.RegisterUser;
using FluentAssertions;
using MediatR;

namespace Pricely.Modules.Users.IntegrationTests.Abstractions;

internal static class CommandHelper
{

    private static readonly Faker _faker = new Faker("fr");
    public static async Task<Guid> RegisterUserAsync(
        this ISender sender, 
        string? email = null, 
        string? userName = null, 
        string? firstName = null, 
        string? lastName = null)
    {
        email ??= _faker.Internet.Email();
        firstName ??= _faker.Name.FirstName();
        lastName ??= _faker.Name.LastName();
        userName ??= $"{firstName}.{lastName}".ToLower(CultureInfo.InvariantCulture);

        var registerCommand = new RegisterUserCommand(email, firstName, lastName, userName);

        Result<Guid> result = await sender.Send(registerCommand);

        result.IsSuccess.Should().BeTrue();

        Guid userId = result.Value;

        return userId;
    }
}
