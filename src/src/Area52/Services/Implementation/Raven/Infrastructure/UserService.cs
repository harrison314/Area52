using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.Role;
using Mcrio.AspNetCore.Identity.On.RavenDb.Model.User;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;

namespace Area52.Services.Implementation.Raven.Infrastructure;

public class UserService : GenericUserServices<RavenIdentityUser, RavenIdentityRole>
{
    public UserService(UserManager<RavenIdentityUser> userManager, RoleManager<RavenIdentityRole> roleManager, ILogger<UserService> logger) 
        : base(userManager, roleManager, logger)
    {
    }

    protected override RavenIdentityRole CreateRoleObject()
    {
        return new RavenIdentityRole();
    }

    protected override RavenIdentityUser CreateUserObject()
    {
        return new RavenIdentityUser();
    }

    protected override Task<bool> DbAnyAsync<T>(IQueryable<T> querable)
    {
       return querable.AnyAsync();
    }
}
