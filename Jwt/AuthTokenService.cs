using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.Extensions.Logging;
using Steam.Models.Utilities;
using TNRD.Zeepkist.GTR.Auth.Directus;
using TNRD.Zeepkist.GTR.Auth.Directus.Models;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public abstract class AuthTokenService : TokenServiceBase
{
    private readonly IDirectusClient client;
    private readonly ILogger logger;

    protected abstract int AuthType { get; }

    /// <inheritdoc />
    public AuthTokenService(IDirectusClient client, ILogger logger)
        : base(client)
    {
        this.client = client;
        this.logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<Result> PersistTokenAsync(TokenResponse response)
    {
        Result<DirectusGetMultipleResponse<AuthModel>> getAuthResult =
            await client.Get<DirectusGetMultipleResponse<AuthModel>>(
                $"items/auth?fields=*.*&filter[user][_eq]={response.UserId}&filter[type][_eq]={AuthType}");

        if (getAuthResult.IsFailed)
        {
            logger.LogCritical("Unable to get auth: {Result}", getAuthResult.ToString());
            return getAuthResult.ToResult();
        }

        Result result;

        if (getAuthResult.Value.HasItems)
        {
            // Patch auth
            AuthModel authModel = getAuthResult.Value.FirstItem!;
            PatchAuthModel patchModel = new PatchAuthModel()
            {
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                AccessTokenExpiry = response.AccessExpiry.ToUniversalTime().ToUnixTimeStamp().ToString(),
                RefreshTokenExpiry = response.RefreshExpiry.ToUniversalTime().ToUnixTimeStamp().ToString()
            };

            Result<DirectusPostResponse<AuthModel>> patchResult =
                await client.Patch<DirectusPostResponse<AuthModel>>($"items/auth/{authModel.Id}?fields=*.*",
                    patchModel);
            result = patchResult.ToResult();
        }
        else
        {
            // Create auth
            PostAuthModel authModel = new PostAuthModel()
            {
                User = response.UserId,
                AuthType = AuthType,
                AccessToken = response.AccessToken,
                RefreshToken = response.RefreshToken,
                AccessTokenExpiry = response.AccessExpiry.ToUniversalTime().ToUnixTimeStamp().ToString(),
                RefreshTokenExpiry = response.RefreshExpiry.ToUniversalTime().ToUnixTimeStamp().ToString()
            };

            Result<DirectusPostResponse<AuthModel>> postResult =
                await client.Post<DirectusPostResponse<AuthModel>>("items/auth?fields=*.*", authModel);
            result = postResult.ToResult();
        }

        if (result.IsFailed)
            logger.LogCritical("Failed to persist token: {Result}", result.ToString());

        return result;
    }

    /// <inheritdoc />
    protected override async Task<Result> RefreshRequestValidationAsync(RefreshTokenRequest request)
    {
        string[] splits = request.UserId.Split('_');
        string userId = splits[0];

        Result<DirectusGetMultipleResponse<AuthModel>> getResult =
            await client.Get<DirectusGetMultipleResponse<AuthModel>>(
                $"items/auth?fields=*.*&filter[user][_eq]={userId}&filter[type][_eq]={AuthType}");

        if (getResult.IsFailed)
        {
            logger.LogCritical("Unable to get auth: {Result}", getResult.ToString());
            return getResult.ToResult();
        }

        if (!getResult.Value.HasItems)
            return Result.Fail("No auth found");

        AuthModel authModel = getResult.Value.FirstItem!;
        if (authModel.RefreshToken != request.RefreshToken)
            return Result.Fail("Refresh token is invalid");

        ulong refreshTimestamp = ulong.Parse(authModel.RefreshTokenExpiry);
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
