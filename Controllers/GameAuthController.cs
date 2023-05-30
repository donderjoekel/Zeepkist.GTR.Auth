using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamUserAuth;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using TNRD.Zeepkist.GTR.Auth.Jwt;
using TNRD.Zeepkist.GTR.Auth.Options;
using TNRD.Zeepkist.GTR.Database;
using TNRD.Zeepkist.GTR.Database.Models;

namespace TNRD.Zeepkist.GTR.Auth.Controllers;

[ApiController]
[Route("game")]
public partial class GameAuthController : ControllerBase
{
    private readonly ILogger<GameAuthController> logger;
    private readonly SteamWebInterfaceFactory factory;
    private readonly SteamOptions steamOptions;
    private readonly GameTokenService gameTokenService;
    private readonly GTRContext context;

    /// <inheritdoc />
    public GameAuthController(
        ILogger<GameAuthController> logger,
        SteamWebInterfaceFactory factory,
        IOptions<SteamOptions> steamOptions,
        GameTokenService gameTokenService,
        GTRContext context
    )
    {
        this.logger = logger;
        this.factory = factory;
        this.gameTokenService = gameTokenService;
        this.context = context;
        this.steamOptions = steamOptions.Value;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(GameLoginRequestModel req)
    {
        if (!VersionUtils.MeetsMinimumVersion(req.ModVersion))
        {
            logger.LogWarning("Trying to refresh token with older version ({Version})", req.ModVersion);
            return Forbid();
        }

        SteamUserAuth userAuth = factory.CreateSteamWebInterface<SteamUserAuth>(new HttpClient());
        ISteamWebResponse<SteamUserAuthResponseModel> authResult =
            await userAuth.AuthenticateUserTicket(steamOptions.AppId, req.AuthenticationTicket);

        if (!authResult.Data.Response.Success)
        {
            logger.LogCritical("Unable to authenticate with steam: {Code};{Description}",
                authResult.Data.Response.Error.ErrorCode,
                authResult.Data.Response.Error.ErrorDesc);
            return Problem("Unable to authenticate with Steam");
        }

        Result<User> userResult = await GetOrCreateUser(req, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        User? user = userResult.Value;

        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await gameTokenService.CreateToken(userId,
            u =>
            {
                u.Claims.Add(new Claim("Type", "0"));
                u.Claims.Add(new Claim("UserId", user.Id.ToString()));
                u.Claims.Add(new Claim("SteamId", user.SteamId));
                u.Roles.Add("game");
            });

        if (result.IsFailed)
            return Problem(result.ToString());

        await UpdateSteamName(user);

        return Ok(result.Value);
    }

    private async Task UpdateSteamName(User user)
    {
        if (string.IsNullOrEmpty(user.SteamId))
            return;

        if (!ulong.TryParse(user.SteamId, out ulong steamId))
            return;

        SteamUser steamUser = factory.CreateSteamWebInterface<SteamUser>(new HttpClient());
        ISteamWebResponse<PlayerSummaryModel> playerSummary = await steamUser.GetPlayerSummaryAsync(steamId);
        string nickname = playerSummary.Data.Nickname;

        User foundUser = await (from u in context.Users
            where u.Id == user.Id
            select u).FirstAsync();

        foundUser.SteamName = nickname;
        await context.SaveChangesAsync();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(GameRefreshRequestModel req)
    {
        if (!VersionUtils.MeetsMinimumVersion(req.ModVersion))
        {
            logger.LogWarning("Trying to refresh token with older version ({Version})", req.ModVersion);
            return Forbid();
        }

        Result<User?> userResult = await GetUser(req, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        if (userResult.Value == null)
            return NotFound();

        Result<bool> validTokenResult =
            await IsValidRefreshToken(userResult.Value.Id, req.RefreshToken, CancellationToken.None);
        if (validTokenResult.IsFailed)
            return Problem(validTokenResult.ToString());

        if (!validTokenResult.Value)
            return Unauthorized();

        User? user = userResult.Value;
        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await gameTokenService.RefreshToken(new RefreshTokenRequest()
        {
            RefreshToken = req.RefreshToken,
            UserId = userId
        });

        if (result.IsFailed)
            return Problem(result.ToString());

        await UpdateSteamName(user);

        return Ok(result.Value);
    }

    private async Task<Result<User?>> GetUser(GameRequestModelBase req, CancellationToken ct)
    {
        User? user = await (from u in context.Users.AsNoTracking()
            where u.SteamId == req.SteamId
            select u).FirstOrDefaultAsync(ct);

        return Result.Ok(user);
    }

    private async Task<Result<User>> CreateUser(GameRequestModelBase req, CancellationToken ct)
    {
        User user = new User()
        {
            SteamId = req.SteamId
        };

        EntityEntry<User> entry = context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        return entry.Entity;
    }

    private async Task<Result<User>> GetOrCreateUser(GameRequestModelBase req, CancellationToken ct)
    {
        Result<User?> getResult = await GetUser(req, ct);

        if (getResult.IsFailed)
            return getResult.ToResult();

        if (getResult.Value != null)
            return getResult.Value;

        return await CreateUser(req, ct);
    }

    private async Task<Result<bool>> IsValidRefreshToken(int userId, string refreshToken, CancellationToken ct)
    {
        Database.Models.Auth? authModel = await (from a in context.Auths.AsNoTracking()
            where a.User == userId && a.Type == 0 && a.RefreshToken == refreshToken
            select a).FirstOrDefaultAsync(ct);

        if (authModel == null)
            return false;

        if (!long.TryParse(authModel.RefreshTokenExpiry, out long expiry))
            return false;

        return DateTimeOffset.FromUnixTimeSeconds(expiry) > DateTime.UtcNow;
    }
}
