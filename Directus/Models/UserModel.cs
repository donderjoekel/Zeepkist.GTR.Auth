using Newtonsoft.Json;

namespace TNRD.Zeepkist.GTR.Auth.Directus.Models;

public class UserModel : BaseDirectusModel
{
    [JsonProperty("steam_id")] public string SteamId { get; set; } = null!;
    [JsonProperty("steam_name")] public string? SteamName { get; set; }
    [JsonProperty("access_token")] public string? AccessToken { get; set; }
    [JsonProperty("access_token_expiry")] public string? AccessTokenExpiry { get; set; }
    [JsonProperty("refresh_token")] public string? RefreshToken { get; set; }
    [JsonProperty("refresh_token_expiry")] public string? RefreshTokenExpiry { get; set; }
}

public class PatchUserSteamNameModel
{
    [JsonProperty("steam_name")] public string SteamName { get; set; } = null!;
}

public class PatchUserAuthModel
{
    [JsonProperty("access_token")] public string AccessToken { get; set; } = null!;
    [JsonProperty("access_token_expiry")] public string AccessTokenExpiry { get; set; } = null!;
    [JsonProperty("refresh_token")] public string RefreshToken { get; set; } = null!;
    [JsonProperty("refresh_token_expiry")] public string RefreshTokenExpiry { get; set; } = null!;
}
