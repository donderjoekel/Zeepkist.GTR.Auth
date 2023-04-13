namespace TNRD.Zeepkist.GTR.Auth.Controllers;

public partial class ExternalAuthController
{
    public class ExternalRefreshRequestModel
    {
        public string SteamId { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
