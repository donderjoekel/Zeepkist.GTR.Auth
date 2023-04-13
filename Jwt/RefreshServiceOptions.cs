using System;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public class RefreshServiceOptions
{
    public string? TokenSigningKey { internal get; set; }
    public TimeSpan AccessTokenValidity { internal get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan RefreshTokenValidity { internal get; set; } = TimeSpan.FromHours(4);
}
