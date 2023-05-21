using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TNRD.Zeepkist.GTR.Auth.Database;
using TNRD.Zeepkist.GTR.Auth.Directus;
using TNRD.Zeepkist.GTR.Auth.Options;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public class ExternalTokenService : AuthTokenService
{
    /// <inheritdoc />
    protected override int AuthType => 1;

    /// <inheritdoc />
    public ExternalTokenService(
        GTRContext context,
        ILogger<ExternalTokenService> logger,
        IOptions<AuthOptions> authOptions
    )
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
        privileges.Roles.Add("external");
    }
}
