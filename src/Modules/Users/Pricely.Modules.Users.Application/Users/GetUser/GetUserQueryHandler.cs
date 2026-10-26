using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;
using Pricely.Modules.Users.Domain.Users;

namespace Pricely.Modules.Users.Application.Users.GetUser;

internal sealed class GetUserQueryHandler(IUserReadStore readStore)
    : IQueryHandler<GetUserQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        UserResponse? user = await readStore.GetAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFound(request.UserId));
        }

        return user;
    }
}
