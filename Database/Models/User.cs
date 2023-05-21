using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class User
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

    public virtual ICollection<Auth> Auths { get; set; } = new List<Auth>();

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Level> Levels { get; set; } = new List<Level>();

    public virtual ICollection<Record> Records { get; set; } = new List<Record>();

    public virtual ICollection<Upvote> Upvotes { get; set; } = new List<Upvote>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
