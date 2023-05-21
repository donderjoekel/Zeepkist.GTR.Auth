using System;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class Upvote
{
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public int? Level { get; set; }

    public int? User { get; set; }

    public virtual Level? LevelNavigation { get; set; }

    public virtual User? UserNavigation { get; set; }
}
