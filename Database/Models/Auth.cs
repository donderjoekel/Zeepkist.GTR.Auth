using System;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class Auth
{
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public int? User { get; set; }

    public string? AccessToken { get; set; }

    public string? AccessTokenExpiry { get; set; }

    public string? RefreshToken { get; set; }

    public string? RefreshTokenExpiry { get; set; }

    public int? Type { get; set; }

    public virtual User? UserNavigation { get; set; }
}
