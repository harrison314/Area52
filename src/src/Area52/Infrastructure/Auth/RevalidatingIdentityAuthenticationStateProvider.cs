using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Area52.Infrastructure.Auth;

//TODO: fix problem with user manager - use custom user srevice
public class RevalidatingIdentityAuthenticationStateProvider<TUser>
    : RevalidatingServerAuthenticationStateProvider where TUser : class
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IdentityOptions options;

    protected override TimeSpan RevalidationInterval
    {
        get => TimeSpan.FromMinutes(30);
    }

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IOptions<IdentityOptions> optionsAccessor)
        : base(loggerFactory)
    {
        this.scopeFactory = scopeFactory;
        this.options = optionsAccessor.Value;
    }

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        try
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<TUser>>();
            return await this.ValidateSecurityStampAsync(userManager, authenticationState.User);
        }
        finally
        {
            if (scope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                scope.Dispose();
            }
        }
    }

    private async Task<bool> ValidateSecurityStampAsync(UserManager<TUser> userManager, ClaimsPrincipal principal)
    {
        TUser user = await userManager.GetUserAsync(principal);
        if (user == null)
        {
            return false;
        }
        else if (!userManager.SupportsUserSecurityStamp)
        {
            return true;
        }
        else
        {
            var principalStamp = principal.FindFirstValue(this.options.ClaimsIdentity.SecurityStampClaimType);
            var userStamp = await userManager.GetSecurityStampAsync(user);
            return principalStamp == userStamp;
        }
    }
}
