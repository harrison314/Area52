﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Area52.Infrastructure.Auth;
using Area52.Services.Contracts;
using Microsoft.AspNetCore.Identity;

namespace Area52.Services.Implementation;

public abstract class GenericUserServices<TUser, TRole> : IUserServices
    where TUser : IdentityUser<string>
    where TRole : IdentityRole<string>
{
    private readonly UserManager<TUser> userManager;
    private readonly RoleManager<TRole> roleManager;
    private readonly ILogger logger;

    public GenericUserServices(UserManager<TUser> userManager, RoleManager<TRole> roleManager, ILogger logger)
    {
        this.userManager = userManager;
        this.roleManager = roleManager;
        this.logger = logger;
    }

    public async Task<bool> TryCreateDefaultLogin(string email, string userName, string password)
    {
        this.logger.LogTrace("Entering to TryCreateDefaultLogin with email {email}, userName {userName}.", email, userName);

        bool anyUser = await this.DbAnyAsync(this.userManager.Users);
        if (anyUser)
        {
            return false;
        }

        TUser user = this.CreateUserObject();
        user.Email = email;
        user.EmailConfirmed = true;
        user.UserName = userName;


        this.CheckIdentityResult(await this.userManager.CreateAsync(user, password));


        this.CheckIdentityResult(await this.userManager.AddToRoleAsync(user, RoleNames.User));
        this.CheckIdentityResult(await this.userManager.AddToRoleAsync(user, RoleNames.Administrator));

        return true;
    }

    public async Task EnshureRoles()
    {
        this.logger.LogTrace("Enshures roles");

        if (!await this.DbAnyAsync(this.roleManager.Roles))
        {
            TRole roleUser = this.CreateRoleObject();
            roleUser.Name = RoleNames.User;

            await this.roleManager.CreateAsync(roleUser);

            TRole roleAdministrator = this.CreateRoleObject();
            roleAdministrator.Name = RoleNames.Administrator;

            await this.roleManager.CreateAsync(roleAdministrator);

            this.logger.LogInformation("Create roles.");
        }
    }

    protected abstract TUser CreateUserObject();

    protected abstract TRole CreateRoleObject();

    protected abstract Task<bool> DbAnyAsync<T>(IQueryable<T> querable);

    private IdentityResult CheckIdentityResult(IdentityResult identityResult)
    {
        if (!identityResult.Succeeded)
        {
            this.logger.LogError("Error during identity operation: {errors}.", string.Join(", ", identityResult.Errors.Select(t => t.Description)));
            throw new Area52Exception($"Error during identity operation: {string.Join(", ", identityResult.Errors.Select(t => t.Description))}");
        }

        return identityResult;
    }

    public async Task<IdentityUser<string>?> GetCurrentUser(ClaimsPrincipal principal)
    {
        TUser? user = await this.userManager.GetUserAsync(principal);
        if (user == null)
        {
            return null;
        }

        if (!this.userManager.SupportsUserSecurityStamp)
        {
            return user;
        }

        string? principalStamp = principal.FindFirstValue(this.userManager.Options.ClaimsIdentity.SecurityStampClaimType);
        string? userStamp = await this.userManager.GetSecurityStampAsync(user);
        if (string.Equals(principalStamp, userStamp, StringComparison.Ordinal))
        {
            return user;
        }

        return null;
    }
}
