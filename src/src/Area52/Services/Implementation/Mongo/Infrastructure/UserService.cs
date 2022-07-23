using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AspNetCore.Identity.Mongo.Model;
using Microsoft.AspNetCore.Identity;

namespace Area52.Services.Implementation.Mongo.Infrastructure;

public class UserService : GenericUserServices<MongoUser<string>, MongoRole<string>>
{
    public UserService(UserManager<MongoUser<string>> userManager, RoleManager<MongoRole<string>> roleManager, ILogger<GenericUserServices<MongoUser<string>, MongoRole<string>>> logger)
        : base(userManager, roleManager, logger)
    {
    }

    protected override MongoRole<string> CreateRoleObject()
    {
        return new MongoRole<string>()
        {
            Id = Guid.NewGuid().ToString()
        };
    }

    protected override MongoUser<string> CreateUserObject()
    {
        return new MongoUser<string>()
        {
            Id = Guid.NewGuid().ToString()
        };
    }

    protected override Task<bool> DbAnyAsync<T>(IQueryable<T> querable)
    {
        bool result = querable.Any();
        return Task.FromResult(result);
    }
}
