using System.Collections.Generic;
using System.Security.Claims;

namespace TNRD.Zeepkist.GTR.Auth.Jwt;

public sealed class UserPrivileges
{
    /// <summary>
    /// claims of the user
    /// </summary>
    public List<Claim> Claims { get; } = new();

    /// <summary>
    /// roles of the user
    /// </summary>
    public List<string> Roles { get; } = new();

    /// <summary>
    /// allowed permissions for the user
    /// </summary>
    public List<string> Permissions { get; } = new();
}
