using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Elden_Ring_File_Analysis_Tool
{
    public static class Util
    {
        public static Dictionary<ulong, string> GetUnkDictionary(string gamePath, string searchPattern)
        {
            return Directory.GetFiles($@"{gamePath}\_unknown", searchPattern, SearchOption.TopDirectoryOnly).GroupBy(s => GetHashFromFilePath(s)).ToDictionary(x => x.Key, x => x.First());
        }

        static Regex _match = new Regex(@"_(?<hash>\d.*\d)");
        public static ulong GetHashFromFilePath(string file)
        {
            Match  hashMatch = _match.Match(Path.GetFileNameWithoutExtension(file));
            string hashString = hashMatch.Groups["hash"].Value.ToString();
            return ulong.Parse(hashString);
        }

        private const uint PRIME = 37;
        private const ulong PRIME64 = 0x85ul;
        public static ulong ComputeHash(string path, bool is64bit)
        {
            string hashable = path.Trim().Replace('\\', '/').ToLowerInvariant();
            if (!hashable.StartsWith("/"))
                hashable = '/' + hashable;

            return is64bit ? hashable.Aggregate(0ul, (i, c) => i * PRIME64 + c) : hashable.Aggregate(0u, (i, c) => i * PRIME + c);
        }
    }
}
