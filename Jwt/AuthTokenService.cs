using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Steam.Models.Utilities;
using TNRD.Zeepkist.GTR.Auth.Database;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public abstract class AuthTokenService : TokenServiceBase
{
    private readonly ILogger logger;
    private readonly GTRContext context;

    protected abstract int AuthType { get; }

    /// <inheritdoc />
    public AuthTokenService(ILogger logger, GTRContext context)
        : base()
    {
        this.logger = logger;
        this.context = context;
    }

    /// <inheritdoc />
    protected override async Task<Result> PersistTokenAsync(TokenResponse response)
    {
        Database.Models.Auth? model = await (from a in context.Auths
            where a.User == response.UserId && a.Type == AuthType
            select a).FirstOrDefaultAsync();

        if (model != null)
        {
            model.AccessToken = response.AccessToken;
            model.RefreshToken = response.RefreshToken;
            model.AccessTokenExpiry = response.AccessExpiry.ToUniversalTime().ToUnixTimeStamp().ToString();
            model.RefreshTokenExpiry = response.RefreshExpiry.ToUniversalTime().ToUnixTimeStamp().ToString();

            EntityEntry<Database.Models.Auth> entry = context.Auths.Update(model);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
        else
        {
            Database.Models.Auth auth = new()
            {
                User = response.UserId,
                Type = AuthType,
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                AccessTokenExpiry = response.AccessExpiry.ToUniversalTime().ToUnixTimeStamp().ToString(),
                RefreshTokenExpiry = response.RefreshExpiry.ToUniversalTime().ToUnixTimeStamp().ToString()
            };

            EntityEntry<Database.Models.Auth> entry = await context.Auths.AddAsync(auth);
            await context.SaveChangesAsync();
            return Result.Ok();
        }
    }

    /// <inheritdoc />
    protected override async Task<Result> RefreshRequestValidationAsync(RefreshTokenRequest request)
    {
        string[] splits = request.UserId.Split('_');
        string userId = splits[0];
        if (!int.TryParse(userId, out int parsedId))
            return Result.Fail("Failed to parse user id");

        Database.Models.Auth? model = await (from a in context.Auths
            where a.User == parsedId && a.Type == AuthType
            select a).FirstOrDefaultAsync();

        if (model == null)
            return Result.Fail("No auth found");

        if (string.IsNullOrEmpty(model.RefreshToken))
            return Result.Fail("User has no refresh token");
        
        if (model.RefreshToken != request.RefreshToken)
            return Result.Fail("Refresh token is invalid");
        
        ulong refreshTimestamp = ulong.Parse(model.RefreshTokenExpiry!);
        DateTime refreshDateTime = refreshTimestamp.ToDateTime();
        if (DateTime.UtcNow > refreshDateTime)
            return Result.Fail("refresh token has expired");
        
        return Result.Ok();
    }

    /// <inheritdoc />
    protected override void SetRenewalPrivilegesAsync(RefreshTokenRequest request, UserPrivileges privileges)
    {
        string[] splits = request.UserId.Split('_');
        privileges.Claims.Add(new Claim("Type", AuthType.ToString()));
        privileges.Claims.Add(new Claim("UserId", splits[0]));
        privileges.Claims.Add(new Claim("SteamId", splits[1]));
    }
}
