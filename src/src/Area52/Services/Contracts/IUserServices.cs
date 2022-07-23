using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Area52.Services.Contracts;

public interface IUserServices
{
    Task EnshureRoles();

    Task<bool> TryCreateDefaultLogin(string email, string userName, string password);
}
