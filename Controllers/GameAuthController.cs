using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steam.Models.SteamUserAuth;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using TNRD.Zeepkist.GTR.Auth.Directus;
using TNRD.Zeepkist.GTR.Auth.Directus.Models;
using TNRD.Zeepkist.GTR.Auth.Jwt;
using TNRD.Zeepkist.GTR.Auth.Options;

namespace TNRD.Zeepkist.GTR.Auth.Controllers;

[ApiController]
[Route("game")]
public partial class GameAuthController : ControllerBase
{
    private readonly ILogger<GameAuthController> logger;
    private readonly IDirectusClient client;
    private readonly SteamWebInterfaceFactory factory;
    private readonly SteamOptions steamOptions;
    private readonly GameTokenService gameTokenService;

    /// <inheritdoc />
    public GameAuthController(
        ILogger<GameAuthController> logger,
        IDirectusClient client,
        SteamWebInterfaceFactory factory,
        IOptions<SteamOptions> steamOptions,
        GameTokenService gameTokenService
    )
    {
        this.logger = logger;
        this.client = client;
        this.factory = factory;
        this.gameTokenService = gameTokenService;
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

        Result<UserModel> userResult = await GetOrCreateUser(req, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        UserModel? user = userResult.Value;

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

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(GameRefreshRequestModel req)
    {
        if (!VersionUtils.MeetsMinimumVersion(req.ModVersion))
        {
            logger.LogWarning("Trying to refresh token with older version ({Version})", req.ModVersion);
            return Forbid();
        }

        Result<UserModel> userResult = await GetOrCreateUser(req, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        UserModel? user = userResult.Value;
        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await gameTokenService.RefreshToken(new RefreshTokenRequest()
        {
            RefreshToken = req.RefreshToken,
            UserId = userId
        });

        if (result.IsFailed)
            return Problem(result.ToString());

        return Ok(result.Value);
    }

    private async Task<Result<UserModel>> GetOrCreateUser(GameRequestModelBase req, CancellationToken ct)
    {
        Result<DirectusGetMultipleResponse<UserModel>> getResult =
            await client.Get<DirectusGetMultipleResponse<UserModel>>(
                $"items/users?fields=*&filter[steam_id][_eq]={req.SteamId}",
                ct);

        if (getResult.IsFailed)
        {
            logger.LogCritical("Unable to check if user exists: {Result}", getResult.ToString());
            return getResult.ToResult();
        }

        if (getResult.Value.HasItems)
            return getResult.Value.FirstItem!;

        UserModel postData = new UserModel() { SteamId = req.SteamId.ToString() };

        Result<DirectusPostResponse<UserModel>> postResult =
            await client.Post<DirectusPostResponse<UserModel>>("items/users", postData, ct);

        if (postResult.IsFailed)
        {
            logger.LogCritical("Unable to create new user: {Result}", postResult.ToString());
            return postResult.ToResult();
        }

        return postResult.Value.Data;
    }
}
