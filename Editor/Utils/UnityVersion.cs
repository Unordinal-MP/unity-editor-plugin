
using System;
using System.Text.RegularExpressions;
namespace Unordinal.Editor.Utils
{
    public struct UnityVersion: IComparable<UnityVersion>, IEquatable<UnityVersion>
    {
        private static readonly Regex VersionPattern = new Regex(@"^(?<major>\d{4})\.(?<minor>\d+)\.(?<patch>\d+)\-?(?<metadata>.+)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string Metadata { get; set; }
        
        public static UnityVersion Parse(string versionString)
        {
            var match = VersionPattern.Match(versionString);
            if (!match.Success) throw new Exception("Invalid Unity version");
            return new UnityVersion
            {
                Major = int.Parse(match.Groups["major"].Value),
                Minor = int.Parse(match.Groups["minor"].Value),
                Patch = int.Parse(match.Groups["patch"].Value),
                Metadata = match.Groups["metadata"].Value
            };
        }

        public int CompareTo(UnityVersion other)
        {
            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;
            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;
            return Patch.CompareTo(other.Patch);
        }

        public static bool operator >(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) > 0;
        }
        
        public static bool operator <(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) < 0;
        }
        
        public static bool operator ==(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) == 0 && left.Metadata == right.Metadata;
        }

        public static bool operator !=(UnityVersion left, UnityVersion right)
        {
            return left.CompareTo(right) != 0 || left.Metadata != right.Metadata;
        }

        public bool Equals(UnityVersion other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
