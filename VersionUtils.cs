using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semver;
using TNRD.Zeepkist.GTR.Database;
using TNRD.Zeepkist.GTR.Database.Models;

namespace TNRD.Zeepkist.GTR.Auth;

public class VersionUtils
{
    public static async Task<bool> MeetsMinimumVersion(GTRContext context, string version)
    {
        if (!SemVersion.TryParse(version, SemVersionStyles.Strict, out SemVersion? semVersion))
            return false;

        Version dbVersion = await context.Versions.AsNoTracking().FirstAsync();
        
        if (!SemVersion.TryParse(dbVersion.Minimum, SemVersionStyles.Strict, out SemVersion? minimumVersion))
            return false;

        return semVersion.ComparePrecedenceTo(minimumVersion) >= 0;
    }
}
