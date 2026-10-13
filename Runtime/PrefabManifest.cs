using System;
using System.Collections.Generic;

namespace PrefabManager
{
    public static class PrefabManifest
    {
        static Dictionary<string, string> _paths = new Dictionary<string, string>(StringComparer.Ordinal);
        internal static IReadOnlyDictionary<string, string> Paths => _paths;
        public static void Register(Dictionary<string, string> paths)
            => _paths = paths != null ? new Dictionary<string, string>(paths, StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal);
        public static bool TryGetPath(string guid, out string path)
        {
            path = null;
            return !string.IsNullOrEmpty(guid) && _paths.TryGetValue(guid, out path);
        }
        public static bool IsRegistered(string guid)
            => !string.IsNullOrEmpty(guid) && _paths.ContainsKey(guid);
        public static int Count => _paths.Count;
        /// <summary>Master switch for [Prefabs] warnings (unknown guid, failed load). Default true.</summary>
        public static bool WarningsEnabled { get; set; } = true;
    }
}
