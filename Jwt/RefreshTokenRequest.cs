namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public class RefreshTokenRequest
{
    // public string ModVersion { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
