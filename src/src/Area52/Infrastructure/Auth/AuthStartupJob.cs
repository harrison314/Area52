using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Area52.Services.Contracts;

namespace Area52.Infrastructure.Auth;

public class AuthStartupJob : IStartupJob
{
    private readonly IUserServices userServices;
    private readonly ILogger<AuthStartupJob> logger;

    public AuthStartupJob(IUserServices userServices, ILogger<AuthStartupJob> logger)
    {
        this.userServices = userServices;
        this.logger = logger;
    }

    public async ValueTask Execute(CancellationToken cancellationToken)
    {
        this.logger.LogTrace("Entering to Execute.");

        await this.userServices.EnshureRoles();

        bool create = await this.userServices.TryCreateDefaultLogin("Administrator@local",
            "Administrator",
            "Password123*");

        if (create)
        {
            this.logger.LogInformation("Create default account.");
        }
    }
}
