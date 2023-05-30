using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TNRD.Zeepkist.GTR.Auth.Options;
using TNRD.Zeepkist.GTR.Database;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public class GameTokenService : AuthTokenService
{
    /// <inheritdoc />
    protected override int AuthType => 0;

    /// <inheritdoc />
    public GameTokenService(GTRContext context, ILogger<GameTokenService> logger, IOptions<AuthOptions> authOptions)
        : base(logger, context)
    {
        Setup(options =>
        {
            options.TokenSigningKey = authOptions.Value.SigningKey;
            options.AccessTokenValidity = TimeSpan.FromMinutes(5);
            options.RefreshTokenValidity = TimeSpan.FromHours(8);
        });
    }

    /// <inheritdoc />
    protected override void SetRenewalPrivilegesAsync(RefreshTokenRequest request, UserPrivileges privileges)
    {
        base.SetRenewalPrivilegesAsync(request, privileges);
        privileges.Roles.Add("game");
    }
}
