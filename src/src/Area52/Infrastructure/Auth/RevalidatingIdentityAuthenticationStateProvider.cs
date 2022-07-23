using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Area52.Infrastructure.Auth;

public class RevalidatingIdentityAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IServiceScopeFactory scopeFactory;

    protected override TimeSpan RevalidationInterval
    {
        get => TimeSpan.FromMinutes(30);
    }

    public RevalidatingIdentityAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory)
        : base(loggerFactory)
    {
        this.scopeFactory = scopeFactory;
    }

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        using IServiceScope scope = this.scopeFactory.CreateScope();
        try
        {
            IUserServices userServices = scope.ServiceProvider.GetRequiredService<IUserServices>();
            return await this.ValidateSecurityStampAsync(userServices, authenticationState.User);
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

    private async Task<bool> ValidateSecurityStampAsync(IUserServices userManager, ClaimsPrincipal principal)
    {
        IdentityUser<string>? user = await userManager.GetCurrentUser(principal);
        return user != null;
    }
}
