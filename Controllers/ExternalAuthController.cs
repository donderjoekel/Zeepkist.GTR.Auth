using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FluentResults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using TNRD.Zeepkist.GTR.Auth.Directus;
using TNRD.Zeepkist.GTR.Auth.Directus.Models;
using TNRD.Zeepkist.GTR.Auth.Jwt;
using TNRD.Zeepkist.GTR.Auth.Options;

namespace TNRD.Zeepkist.GTR.Auth.Controllers;

[ApiController]
[Route("external")]
public partial class ExternalAuthController : ControllerBase
{
    private readonly ILogger<ExternalAuthController> logger;
    private readonly IDirectusClient client;
    private readonly SteamWebInterfaceFactory factory;
    private readonly SteamOptions steamOptions;
    private readonly ExternalTokenService externalTokenService;

    /// <inheritdoc />
    public ExternalAuthController(
        ILogger<ExternalAuthController> logger,
        IDirectusClient client,
        SteamWebInterfaceFactory factory,
        IOptions<SteamOptions> steamOptions,
        ExternalTokenService externalTokenService
    )
    {
        this.logger = logger;
        this.client = client;
        this.factory = factory;
        this.steamOptions = steamOptions.Value;
        this.externalTokenService = externalTokenService;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string redirectUrl)
    {
        return Challenge(new AuthenticationProperties()
            {
                RedirectUri = "/external/confirm?redirectUrl=" + HttpUtility.UrlEncode(redirectUrl)
            },
            "Steam");
    }

    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm([FromQuery] string redirectUrl)
    {
        const string claimIdentifier = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";

        if (!User.Identity?.IsAuthenticated ?? false)
            return Unauthorized();

        Claim? claim = HttpContext.User.FindFirst(claimIdentifier);
        if (claim == null)
            return Unauthorized();

        string steamId = claim.Value[(claim.Value.LastIndexOf('/') + 1)..];
        if (string.IsNullOrEmpty(steamId))
            return BadRequest();

        PlayerService playerService = factory.CreateSteamWebInterface<PlayerService>();
        ISteamWebResponse<OwnedGamesResultModel> ownedGamesResponse = await playerService.GetOwnedGamesAsync(
            ulong.Parse(steamId),
            false,
            true,
            new List<uint>()
            {
                steamOptions.AppId
            });

        if (ownedGamesResponse == null)
            return Problem();

        if (ownedGamesResponse.Data.GameCount != 1)
            return Forbid();

        Result<UserModel> userResult = await GetOrCreateUser(steamId, CancellationToken.None);
        if (userResult.IsFailed)
            return Problem();

        UserModel? user = userResult.Value;
        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await externalTokenService.CreateToken(userId,
            u =>
            {
                u.Claims.Add(new Claim("Type", "1"));
                u.Claims.Add(new Claim("UserId", user.Id.ToString()));
                u.Claims.Add(new Claim("SteamId", user.SteamId));
                u.Roles.Add("external");
            });

        if (result.IsFailed)
            return Problem(result.ToString());

        string json = JsonConvert.SerializeObject(result.Value);
        byte[] jsonBytes = Encoding.Default.GetBytes(json);
        string b64 = Convert.ToBase64String(jsonBytes);

        string url = HttpUtility.UrlDecode(redirectUrl) + "?token=" + b64;
        return Redirect(url);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(ExternalRefreshRequestModel req)
    {
        Result<UserModel?> userResult = await GetUser(req.SteamId, CancellationToken.None);

        if (userResult.IsFailed)
            return Problem(userResult.ToString());

        if (userResult.Value == null)
            return NotFound();
        
        Result<bool> validTokenResult =  await IsValidRefreshToken(userResult.Value.Id, req.RefreshToken, CancellationToken.None);
        if (validTokenResult.IsFailed)
            return Problem(validTokenResult.ToString());

        if (!validTokenResult.Value)
            return Unauthorized();

        UserModel? user = userResult.Value;
        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await externalTokenService.RefreshToken(new RefreshTokenRequest()
        {
            RefreshToken = req.RefreshToken,
            UserId = userId
        });

        if (result.IsFailed)
            return Problem(result.ToString());

        return Ok(result.Value);
    }

    private async Task<Result<UserModel?>> GetUser(string steamId, CancellationToken ct)
    {
        Result<DirectusGetMultipleResponse<UserModel>> getResult =
            await client.Get<DirectusGetMultipleResponse<UserModel>>(
                $"items/users?fields=*&filter[steam_id][_eq]={steamId}",
                ct);

        if (getResult.IsFailed)
        {
            logger.LogCritical("Unable to check if user exists: {Result}", getResult.ToString());
            return getResult.ToResult();
        }

        return getResult.Value.HasItems ? getResult.Value.FirstItem! : Result.Ok();
    }

    private async Task<Result<UserModel>> CreateUser(string steamId, CancellationToken ct)
    {
        UserModel postData = new UserModel() { SteamId = steamId };

        Result<DirectusPostResponse<UserModel>> postResult =
            await client.Post<DirectusPostResponse<UserModel>>("items/users", postData, ct);

        if (postResult.IsFailed)
        {
            logger.LogCritical("Unable to create new user: {Result}", postResult.ToString());
            return postResult.ToResult();
        }

        return postResult.Value.Data;
    }

    private async Task<Result<UserModel>> GetOrCreateUser(string steamId, CancellationToken ct)
    {
        Result<UserModel?> getResult = await GetUser(steamId, ct);

        if (getResult.IsFailed)
            return getResult.ToResult();

        if (getResult.Value != null)
            return getResult.Value;

        return await CreateUser(steamId, ct);
    }

    private async Task<Result<bool>> IsValidRefreshToken(int userId, string refreshToken, CancellationToken ct)
    {
        Result<DirectusGetMultipleResponse<AuthModel>> result =
            await client.Get<DirectusGetMultipleResponse<AuthModel>>(
                $"items/auth?fields=*&filter[user_id][_eq]={userId}&filter[type][_eq]=1&filter[refresh_token][_eq]={refreshToken}",
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
