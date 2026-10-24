using System;
using System.Collections.Generic;
using System.Text;

namespace Pricely.Modules.Users.Application.Users;


public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName);
