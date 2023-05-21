using System;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class Record
{
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public int? Level { get; set; }

    public int? User { get; set; }

    public float? Time { get; set; }

    public bool IsBest { get; set; }

    public string? Splits { get; set; }

    public string? GhostUrl { get; set; }

    public string? ScreenshotUrl { get; set; }

    public string? GameVersion { get; set; }

    public bool IsValid { get; set; }

    public bool IsWr { get; set; }

    public virtual Level? LevelNavigation { get; set; }

    public virtual User? UserNavigation { get; set; }
}
