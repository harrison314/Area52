using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Area52.Services.Contracts;

public interface IUserServices
{
    Task EnshureRoles();

    Task<bool> TryCreateDefaultLogin(string email, string userName, string password);

    Task<IdentityUser<string>?> GetCurrentUser(ClaimsPrincipal principal);
}
