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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Steam.Models.SteamCommunity;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;
using TNRD.Zeepkist.GTR.Auth.Jwt;
using TNRD.Zeepkist.GTR.Auth.Options;
using TNRD.Zeepkist.GTR.Database;
using TNRD.Zeepkist.GTR.Database.Models;

namespace TNRD.Zeepkist.GTR.Auth.Controllers;

[ApiController]
[Route("external")]
public partial class ExternalAuthController : ControllerBase
{
    private readonly ILogger<ExternalAuthController> logger;
    private readonly SteamWebInterfaceFactory factory;
    private readonly SteamOptions steamOptions;
    private readonly ExternalTokenService externalTokenService;
    private readonly GTRContext context;

    /// <inheritdoc />
    public ExternalAuthController(
        ILogger<ExternalAuthController> logger,
        SteamWebInterfaceFactory factory,
        IOptions<SteamOptions> steamOptions,
        ExternalTokenService externalTokenService,
        GTRContext context
    )
    {
        this.logger = logger;
        this.factory = factory;
        this.steamOptions = steamOptions.Value;
        this.externalTokenService = externalTokenService;
        this.context = context;
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

        // TODO: Removing this check for now as it seems to break things, also it isn't super necessary
        
        // PlayerService playerService = factory.CreateSteamWebInterface<PlayerService>();
        // ISteamWebResponse<OwnedGamesResultModel> ownedGamesResponse = await playerService.GetOwnedGamesAsync(
        //     ulong.Parse(steamId),
        //     false,
        //     true,
        //     new List<uint>()
        //     {
        //         steamOptions.AppId
        //     });
        //
        // if (ownedGamesResponse == null)
        //     return Problem();
        //
        // if (ownedGamesResponse.Data.GameCount != 1)
        //     return Forbid();

        User user = await GetOrCreateUser(steamId, CancellationToken.None);
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
        User? user = await GetUser(req.SteamId, CancellationToken.None);

        if (user == null)
            return NotFound();

        Result<bool> validTokenResult =
            await IsValidRefreshToken(user.Id, req.RefreshToken, CancellationToken.None);
        if (validTokenResult.IsFailed)
            return Problem(validTokenResult.ToString());

        if (!validTokenResult.Value)
            return Unauthorized();

        string userId = $"{user.Id}_{user.SteamId}";

        Result<TokenResponse> result = await externalTokenService.RefreshToken(new RefreshTokenRequest()
        {
            RefreshToken = req.RefreshToken,
            UserId = userId
        });

        if (result.IsFailed)
            return Problem(result.ToString());

        string json = JsonConvert.SerializeObject(result.Value);
        return Ok(json);
    }

    private async Task<User?> GetUser(string steamId, CancellationToken ct)
    {
        return await context.Users.FirstOrDefaultAsync(x => x.SteamId == steamId, cancellationToken: ct);
    }

    private async Task<User> CreateUser(string steamId, CancellationToken ct)
    {
        User user = new User()
        {
            SteamId = steamId
        };

        EntityEntry<User> entry = context.Users.Add(user);
        await context.SaveChangesAsync(ct);

        return entry.Entity;
    }

    private async Task<User> GetOrCreateUser(string steamId, CancellationToken ct)
    {
        User? user = await GetUser(steamId, ct);

        if (user != null)
            return user;

        return await CreateUser(steamId, ct);
    }

    private async Task<Result<bool>> IsValidRefreshToken(int userId, string refreshToken, CancellationToken ct)
    {
        Database.Models.Auth? auth = await context.Auths.AsNoTracking()
            .FirstOrDefaultAsync(x => x.User == userId && x.Type == 1 && x.RefreshToken == refreshToken, ct);

        if (auth == null)
            return false;

        if (!long.TryParse(auth.RefreshTokenExpiry, out long expiry))
            return false;

        return DateTimeOffset.FromUnixTimeSeconds(expiry) > DateTime.UtcNow;
    }
}
