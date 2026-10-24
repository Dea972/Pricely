using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.RegisterUser;

public sealed record RegisterUserCommand(string Email, string UserName,  string FirstName, string LastName)
    : ICommand<Guid>;
