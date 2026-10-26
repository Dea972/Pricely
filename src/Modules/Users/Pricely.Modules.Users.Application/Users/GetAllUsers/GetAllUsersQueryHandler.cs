using Pricely.Common.Application.Messaging;
using Pricely.Common.Domain;

namespace Pricely.Modules.Users.Application.Users.GetAllUsers;

internal sealed class GetAllUsersQueryHandler(IUserReadStore readStore)
    : IQueryHandler<GetAllUsersQuery, IEnumerable<UserDetailResponse>>
{
    public async Task<Result<IEnumerable<UserDetailResponse>>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken) =>
        Result.Success<IEnumerable<UserDetailResponse>>(await readStore.GetAllAsync(cancellationToken));
}
