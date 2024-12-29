

using System.Text.RegularExpressions;

namespace MagicFunnel;
public class SemVer
{
    public int major;
    public int minor;
    public int patch;

    public SemVer(int major, int minor, int patch)
    {
        this.major = major;
        this.minor = minor;
        this.patch = patch;
    }

    private SemVer() { }

    public static SemVer Parse(string semVer)
    {
        if (semVer.Count(x => x == '.') != 2) throw new ArgumentException("Invalid SemVer");
        string[] strings = semVer.Split('.');
        string major = strings[0], minor = strings[1], patch = strings[2];
        SemVer semver = new();
        if (!int.TryParse(major, out semver.major)) throw new ArgumentException("Invalid SemVer");
        if (!int.TryParse(minor, out semver.minor)) throw new ArgumentException("Invalid SemVer");
        if (!int.TryParse(patch, out semver.patch)) throw new ArgumentException("Invalid SemVer");

        return semver;
    }

    public static SemVer? TryParse(string semVer)
    {
        if (semVer.Count(x => x == '.') != 2) return null;
        string[] strings = semVer.Split('.');
        string major = strings[0], minor = strings[1], patch = strings[2];
        SemVer semver = new();
        if (!int.TryParse(major, out semver.major)) return null;
        if (!int.TryParse(minor, out semver.minor)) return null;
        if (!int.TryParse(patch, out semver.patch)) return null;

        return semver;
    }

    static readonly Regex semverRegex = new(@"\d+\.\d+\.\d+");
    public static SemVer? ParseBetwixt(string str)
    {
        // do some regex magic
        Match match = semverRegex.Match(str);
        if (match.Success)
        {
            return Parse(match.Value);
        }

        return null;
    }

    public static bool operator >(SemVer a, SemVer b)
    {
        if (a.major > b.major) return true;
        if (a.minor > b.minor) return true;
        if (a.patch > b.patch) return true;
        return false;
    }

    public static bool operator <(SemVer a, SemVer b)
    {
        if (a.major < b.major) return true;
        if (a.minor < b.minor) return true;
        if (a.patch < b.patch) return true;
        return false;
    }

    public static bool operator >=(SemVer a, SemVer b)
    {
        if (a.major < b.major) return false;
        if (a.minor < b.minor) return false;
        if (a.patch < b.patch) return false;
        return true;
    }

    public static bool operator <=(SemVer a, SemVer b)
    {
        if (a.major > b.major) return false;
        if (a.minor > b.minor) return false;
        if (a.patch > b.patch) return false;
        return true;
    }
}
