using System;
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

        Result<UserModel> userResult = await GetOrCreateUser(req, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        UserModel? user = userResult.Value;

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

        Result<UserModel?> userResult = await GetUser(req, CancellationToken.None);
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

    private async Task<Result<UserModel?>> GetUser(GameRequestModelBase req, CancellationToken ct)
    {
        Result<DirectusGetMultipleResponse<UserModel>> getResult =
            await client.Get<DirectusGetMultipleResponse<UserModel>>(
                $"items/users?fields=*.*&filter[steam_id][_eq]={req.SteamId}",
                ct);

        if (getResult.IsFailed)
        {
            logger.LogCritical("Unable to check if user exists: {Result}", getResult.ToString());
            return getResult.ToResult();
        }

        return getResult.Value.HasItems ? getResult.Value.FirstItem! : Result.Ok();
    }

    private async Task<Result<UserModel>> CreateUser(GameRequestModelBase req, CancellationToken ct)
    {
        UserModel postData = new UserModel() { SteamId = req.SteamId };

        Result<DirectusPostResponse<UserModel>> postResult =
            await client.Post<DirectusPostResponse<UserModel>>("items/users", postData, ct);

        if (postResult.IsFailed)
        {
            logger.LogCritical("Unable to create new user: {Result}", postResult.ToString());
            return postResult.ToResult();
        }

        return postResult.Value.Data;
    }

    private async Task<Result<UserModel>> GetOrCreateUser(GameRequestModelBase req, CancellationToken ct)
    {
        Result<UserModel?> getResult = await GetUser(req, ct);

        if (getResult.IsFailed)
            return getResult.ToResult();

        if (getResult.Value != null)
            return getResult.Value;

        return await CreateUser(req, ct);
    }

    private async Task<Result<bool>> IsValidRefreshToken(int userId, string refreshToken, CancellationToken ct)
    {
        Result<DirectusGetMultipleResponse<AuthModel>> result =
            await client.Get<DirectusGetMultipleResponse<AuthModel>>(
                $"items/auth?fields=*.*&filter[user][_eq]={userId}&filter[type][_eq]=0&filter[refresh_token][_eq]={refreshToken}",
                ct);

        if (result.IsFailed)
        {
            logger.LogCritical("Unable to check for valid refresh token");
            return result.ToResult();
        }

        if (!result.Value.HasItems)
            return false;

        if (!long.TryParse(result.Value.FirstItem!.RefreshTokenExpiry, out long expiry))
            return false;

        return DateTimeOffset.FromUnixTimeSeconds(expiry) > DateTime.UtcNow;
    }
}
