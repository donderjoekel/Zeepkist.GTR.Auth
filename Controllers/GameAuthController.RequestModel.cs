namespace TNRD.Zeepkist.GTR.Auth.Controllers;

public partial class GameAuthController
{
    public abstract class GameRequestModelBase
    {
        public string ModVersion { get; set; } = null!;
        public string SteamId { get; set; } = null!;
    }

    public class GameLoginRequestModel : GameRequestModelBase
    {
        public string AuthenticationTicket { get; set; } = null!;
    }

    public class GameRefreshRequestModel : GameRequestModelBase
    {
        public string RefreshToken { get; set; } = null!;
    }
}
