using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.GetAllUsers;

public sealed record GetAllUsersQuery : IQuery<IEnumerable<UserDetailResponse>>;
