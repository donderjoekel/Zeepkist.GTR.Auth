using System;
using System.Collections.Generic;

namespace TNRD.Zeepkist.GTR.Auth.Database.Models;

public partial class Level
{
    public int Id { get; set; }

    public DateTime? DateCreated { get; set; }

    public DateTime? DateUpdated { get; set; }

    public string? Uid { get; set; }

    public string? Name { get; set; }

    public string? Author { get; set; }

    public float? TimeAuthor { get; set; }

    public float TimeGold { get; set; }

    public float? TimeSilver { get; set; }

    public float? TimeBronze { get; set; }

    public int CreatedBy { get; set; }

    public string? Wid { get; set; }

    public bool? IsValid { get; set; }

    public string? ThumbnailUrl { get; set; }

    public int? Rank { get; set; }

    public int? Points { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<Record> Records { get; set; } = new List<Record>();
    public virtual ICollection<Upvote> Upvotes { get; set; } = new List<Upvote>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public class EqualityComparer : IEqualityComparer<Level>
    {
        public bool Equals(Level? x, Level? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Id == y.Id;
        }

        public int GetHashCode(Level obj)
        {
            return obj.Id;
        }
    }
}
