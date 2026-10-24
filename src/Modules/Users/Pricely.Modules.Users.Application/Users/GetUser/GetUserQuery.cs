using System;
using System.Collections.Generic;
using System.Text;
using Pricely.Common.Application.Messaging;

namespace Pricely.Modules.Users.Application.Users.GetUser;

public sealed record class GetUserQuery(Guid UserId) : IQuery<UserResponse>;
