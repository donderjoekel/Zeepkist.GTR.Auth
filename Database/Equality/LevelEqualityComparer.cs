using System.Collections.Generic;
using TNRD.Zeepkist.GTR.Auth.Database.Models;

namespace TNRD.Zeepkist.GTR.Auth.Database.Equality;

internal class LevelEqualityComparer : IEqualityComparer<Level>
{
    public static LevelEqualityComparer Instance { get; } = new();

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
