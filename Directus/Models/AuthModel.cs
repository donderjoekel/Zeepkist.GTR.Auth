using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus.Models;

public class AuthModel : BaseDirectusModel
{
    [JsonProperty("user")] public UserModel User { get; set; } = null!;
    [JsonProperty("type")] public int AuthType { get; set; }
    [JsonProperty("access_token")] public string? AccessToken { get; set; }
    [JsonProperty("refresh_token")] public string? RefreshToken { get; set; }
    [JsonProperty("access_token_expiry")] public string? AccessTokenExpiry { get; set; }
    [JsonProperty("refresh_token_expiry")] public string? RefreshTokenExpiry { get; set; }
}

public class PatchAuthModel
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = null!;
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = null!;
    [JsonProperty("access_token_expiry")] public string AccessTokenExpiry { get; set; } = null!;
    [JsonProperty("refresh_token_expiry")] public string RefreshTokenExpiry { get; set; } = null!;
}

public class PostAuthModel : PatchAuthModel
{
    [JsonProperty("user")] public int User { get; set; }
    [JsonProperty("type")] public int AuthType { get; set; }
}
