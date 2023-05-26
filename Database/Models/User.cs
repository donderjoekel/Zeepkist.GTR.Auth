using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class User : IModel
{
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public string? SteamId { get; set; }

    public string? SteamName { get; set; }

    public string? DiscordId { get; set; }

    public int? Position { get; set; }

    public float? Score { get; set; }

    public int? WorldRecords { get; set; }

    public bool? Banned { get; set; }

    public virtual ICollection<Auth> Auths { get; set; } = new List<Auth>();
}
