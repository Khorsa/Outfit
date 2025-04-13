using OutfitTool.Services.HotkeyManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace OutfitTool.Services.Updates
{
    class Version
    {
        public int MajorVersion;
        public int MinorVersion;


        public Version(
            int MajorVersion,
            int MinorVersion
            )
        {
            this.MajorVersion = MajorVersion;
            this.MinorVersion = MinorVersion;
        }

        public static Version FromString(string version)
        {
            return new Version(
                int.Parse(version[..version.IndexOf(".")]),
                int.Parse(version[(version.IndexOf('.') + 1)..])
            );
        }

        public override string ToString()
        {
            return MajorVersion + "." + MinorVersion;
        }

        public static bool operator <(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion < b.MajorVersion * 10000 + b.MinorVersion;
        }
        public static bool operator >(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion > b.MajorVersion * 10000 + b.MinorVersion;
        }
        public static bool operator <=(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion <= b.MajorVersion * 10000 + b.MinorVersion;
        }
        public static bool operator >=(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion >= b.MajorVersion * 10000 + b.MinorVersion;
        }
        public static bool operator ==(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion == b.MajorVersion * 10000 + b.MinorVersion;
        }
        public static bool operator !=(Version a, Version b)
        {
            return a.MajorVersion * 10000 + a.MinorVersion != b.MajorVersion * 10000 + b.MinorVersion;
        }

    }
}
