using System;
using System.Collections.Generic;

using PrefabManager.Entities;

using UnityEngine;

namespace PrefabManager
{
    public static class PrefabManifest
    {
        static Dictionary<string, string> _paths = new Dictionary<string, string>(StringComparer.Ordinal);
        internal static IReadOnlyDictionary<string, string> Paths => _paths;
        static readonly Dictionary<string, string> _overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        internal static event Action OverridesChanged;
        public static IReadOnlyDictionary<string, string> Overrides => _overrides;
        public static int OverrideCount => _overrides.Count;
        public static void Register(Dictionary<string, string> paths)
            => _paths = paths != null ? new Dictionary<string, string>(paths, StringComparer.Ordinal) : new Dictionary<string, string>(StringComparer.Ordinal);
        /// <summary>Path of the effective guid (override-aware). For reskin/debugging.</summary>
        public static bool TryGetPath(string guid, out string path)
        {
            path = null;
            if (string.IsNullOrEmpty(guid)) return false;
            return _paths.TryGetValue(ResolveGuid(guid), out path);
        }
        public static bool IsRegistered(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            return _paths.ContainsKey(ResolveGuid(guid));
        }

        /// <summary>Follow the override chain (max 16 hops, cycle-safe). No override → same guid.</summary>
        public static string ResolveGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid) || _overrides.Count == 0) return guid;
            var current = guid;
            HashSet<string> visited = null;
            for (int i = 0; i < 16; i++)
            {
                if (!_overrides.TryGetValue(current, out var next) || string.IsNullOrEmpty(next))
                    return current;
                if (string.Equals(next, current, StringComparison.Ordinal))
                    return current;
                visited ??= new HashSet<string>(StringComparer.Ordinal) { guid };
                if (!visited.Add(next))
                {
                    if (WarningsEnabled)
                        Debug.LogWarning($"[Prefabs] Override cycle detected at '{next}' (from '{guid}').");
                    return current;
                }
                current = next;
            }
            if (WarningsEnabled)
                Debug.LogWarning($"[Prefabs] Override chain too deep from '{guid}'.");
            return current;
        }

        /// <summary>Direct (non-transitive) override lookup for tools/debugging.</summary>
        public static bool TryGetOverride(string originalGuid, out string replacementGuid)
        {
            replacementGuid = null;
            return !string.IsNullOrEmpty(originalGuid) && _overrides.TryGetValue(originalGuid, out replacementGuid);
        }

        /// <summary>Swap one prefab for another at runtime (reskin). Gameplay code keeps the old ref.</summary>
        public static void SetOverride(string originalGuid, string replacementGuid)
        {
            if (SetOverrideCore(originalGuid, replacementGuid))
                OverridesChanged?.Invoke();
        }
        public static void SetOverride(PrefabRef original, PrefabRef replacement)
            => SetOverride(original.Guid, replacement.Guid);

        /// <summary>Apply a whole group/skin at once (one cache invalidation). Layer over existing.</summary>
        public static void SetOverrides(IEnumerable<KeyValuePair<string, string>> overrides)
        {
            if (overrides == null) return;
            bool changed = false;
            foreach (var kv in overrides)
                changed |= SetOverrideCore(kv.Key, kv.Value);
            if (changed)
                OverridesChanged?.Invoke();
        }
        public static void SetOverrides(IEnumerable<KeyValuePair<PrefabRef, PrefabRef>> overrides)
        {
            if (overrides == null) return;
            bool changed = false;
            foreach (var kv in overrides)
                changed |= SetOverrideCore(kv.Key.Guid, kv.Value.Guid);
            if (changed)
                OverridesChanged?.Invoke();
        }

        public static bool ClearOverride(string originalGuid)
        {
            if (string.IsNullOrEmpty(originalGuid) || !_overrides.Remove(originalGuid)) return false;
            OverridesChanged?.Invoke();
            return true;
        }
        public static bool ClearOverride(PrefabRef original)
            => ClearOverride(original.Guid);
        public static void ClearAllOverrides()
        {
            if (_overrides.Count == 0) return;
            _overrides.Clear();
            OverridesChanged?.Invoke();
        }

        static bool SetOverrideCore(string originalGuid, string replacementGuid)
        {
            if (string.IsNullOrEmpty(originalGuid)) return false;
            if (string.IsNullOrEmpty(replacementGuid) ||
                string.Equals(originalGuid, replacementGuid, StringComparison.Ordinal))
                return _overrides.Remove(originalGuid);
            if (_overrides.TryGetValue(originalGuid, out var existing) &&
                string.Equals(existing, replacementGuid, StringComparison.Ordinal))
                return false;
            _overrides[originalGuid] = replacementGuid;
            return true;
        }
        public static int Count => _paths.Count;
        /// <summary>Master switch for [Prefabs] warnings (unknown guid, failed load). Default true.</summary>
        public static bool WarningsEnabled { get; set; } = true;
    }
}
