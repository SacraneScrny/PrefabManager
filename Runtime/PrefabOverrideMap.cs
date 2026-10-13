using System;
using System.Collections.Generic;
using System.Linq;

using PrefabManager.Entities;

using UnityEngine;

namespace PrefabManager
{
    /// <summary>
    /// Editor-friendly reskin: maps original prefabs to replacements.
    /// Maps anywhere under a Resources folder auto-apply at startup
    /// (sorted by <see cref="priority"/>, then name); others are applied manually via <see cref="Apply"/>.
    /// Rows are toggled individually (unchecked rows are skipped).
    /// A conflict (same original → different replacement) overwrites with a warning; higher priority wins.
    /// Stores only GUIDs — the map itself never references prefab assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Prefabs/Override Map", fileName = "PrefabOverrideMap")]
    public sealed class PrefabOverrideMap : ScriptableObject
    {
        [Tooltip("Lower applies first; higher wins conflicts.")]
        public int priority;
        [Tooltip("Original → replacement. Empty-replacement rows are skipped with a warning.")]
        public List<Entry> entries = new List<Entry>();

        [Serializable]
        public struct Entry
        {
            public bool disabled;
            public PrefabRef Original;
            public PrefabRef Replacement;
        }

        public void Apply() => ApplyAll(new[] { this });

        public void Revert()
        {
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e.disabled) continue;
                var orig = e.Original.Guid;
                if (string.IsNullOrEmpty(orig)) continue;
                if (PrefabManifest.TryGetOverride(orig, out var existing) &&
                    string.Equals(existing, e.Replacement.Guid, StringComparison.Ordinal))
                    PrefabManifest.ClearOverride(orig);
            }
        }

        public static void ApplyAll(IEnumerable<PrefabOverrideMap> maps)
        {
            if (maps == null) return;
            var ordered = maps
                .Where(m => m != null)
                .OrderBy(m => m.priority)
                .ThenBy(m => m.name, StringComparer.Ordinal);
            foreach (var map in ordered)
                map.ApplyEntries();
        }

        void ApplyEntries()
        {
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (e.disabled) continue;
                var orig = e.Original.Guid;
                var repl = e.Replacement.Guid;
                if (string.IsNullOrEmpty(orig)) continue;
                if (string.IsNullOrEmpty(repl))
                {
                    Debug.LogWarning($"[Prefabs] Empty replacement for '{Describe(orig)}' in map '{name}' — skipped.");
                    continue;
                }
                if (string.Equals(orig, repl, StringComparison.Ordinal)) continue;
                if (PrefabManifest.TryGetOverride(orig, out var existing))
                {
                    if (string.Equals(existing, repl, StringComparison.Ordinal)) continue;
                    Debug.LogWarning($"[Prefabs] Override conflict for '{Describe(orig)}': " +
                                     $"'{Describe(existing)}' overwritten by '{Describe(repl)}' (map '{name}').");
                }
                PrefabManifest.SetOverride(orig, repl);
            }
        }

        static string Describe(string guid)
            => PrefabManifest.Paths.TryGetValue(guid, out var path) ? $"{path} ({guid})" : guid;
    }

    internal static class PrefabOverrideMapBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void AutoApply()
        {
            var maps = Resources.LoadAll<PrefabOverrideMap>(string.Empty);
            if (maps == null || maps.Length == 0) return;
            PrefabManifest.ClearAllOverrides();
            PrefabOverrideMap.ApplyAll(maps);
            Debug.Log($"[Prefabs] Applied {maps.Length} override maps, {PrefabManifest.OverrideCount} active overrides.");
        }
    }
}
