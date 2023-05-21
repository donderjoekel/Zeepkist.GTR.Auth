using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
using Microsoft.IdentityModel.Tokens;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public abstract class TokenServiceBase
{
    private RefreshServiceOptions? opts;

    protected TokenServiceBase()
    {
    }

    public void Setup(Action<RefreshServiceOptions> options)
    {
        opts = new();
        options(opts);
    }

    protected abstract Task<Result> PersistTokenAsync(TokenResponse response);
    protected abstract Task<Result> RefreshRequestValidationAsync(RefreshTokenRequest req);
    protected abstract void SetRenewalPrivilegesAsync(RefreshTokenRequest req, UserPrivileges privileges);

    public async Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest req)
    {
        Result refreshResult = await RefreshRequestValidationAsync(req);
        if (refreshResult.IsFailed)
            return refreshResult;

        Result<TokenResponse> response = await CreateToken(
            req.UserId,
            p => SetRenewalPrivilegesAsync(req, p));

        return response;
    }

    public async Task<Result<TokenResponse>> CreateToken(string userId, Action<UserPrivileges> userPrivileges)
    {
        if (opts?.TokenSigningKey is null)
            throw new ArgumentNullException($"{nameof(opts.TokenSigningKey)} must be specified]");

        UserPrivileges privileges = new UserPrivileges();
        userPrivileges(privileges);

        DateTime accessExpiry = DateTime.UtcNow.Add(opts.AccessTokenValidity);
        DateTime refreshExpiry = DateTime.UtcNow.Add(opts.RefreshTokenValidity);
        TokenResponse token = new TokenResponse()
        {
            UserId = int.Parse(userId.Split('_')[0]),
            SteamId = userId.Split('_')[1],
            AccessToken = CreateToken(
                opts.TokenSigningKey,
                accessExpiry,
                privileges.Roles,
                privileges.Claims),
            AccessExpiry = accessExpiry,
            RefreshToken = Guid.NewGuid().ToString("N"),
            RefreshExpiry = refreshExpiry
        };

        Result persistResult = await PersistTokenAsync(token);
        if (persistResult.IsFailed)
            return persistResult;

        return token;
    }

    private static string CreateToken(
        string signingKey,
        DateTime? expireAt = null,
        IEnumerable<string>? roles = null,
        IEnumerable<Claim>? claims = null,
        string? issuer = null,
        string? audience = null
    )
    {
        List<Claim> claimList = new List<Claim>();

        if (claims != null)
            claimList.AddRange(claims);

        if (roles != null)
            claimList.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            IssuedAt = DateTime.UtcNow,
            Subject = new ClaimsIdentity(claimList),
            Expires = expireAt,
            SigningCredentials = GetSigningCredentials(signingKey)
        };

        JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private static SigningCredentials GetSigningCredentials(string key)
    {
        return new SigningCredentials(
            new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
            SecurityAlgorithms.HmacSha256Signature);
    }
}
