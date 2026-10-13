using System;

using UnityEditor;

namespace PrefabManager.Editor
{
    /// <summary>
    /// Keeps guid->path manifest fresh on prefab add/delete/move/rename.
    /// Identity is the GUID, so serialized PrefabRef stays valid —
    /// only the mapping needs regeneration.
    /// </summary>
    public sealed class PrefabPostprocessor : AssetPostprocessor
    {
        static bool _scheduled;
        static bool _generating;

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_generating) return;
            if (!NeedsRegen(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths)) return;
            if (_scheduled) return;

            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                if (_generating) return;
                try
                {
                    _generating = true;
                    PrefabGenerator.DoGenerate(refresh: false);
                }
                finally
                {
                    _generating = false;
                }
            };
        }

        static bool NeedsRegen(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            return IsPrefabChange(imported)
                || IsPrefabChange(deleted)
                || IsPrefabChange(moved)
                || IsPrefabChange(movedFrom);
        }

        static bool IsPrefabChange(string[] paths)
        {
            if (paths == null) return false;
            foreach (var p in paths)
            {
                if (string.IsNullOrEmpty(p)) continue;
                if (PrefabGenerator.IsGeneratedPath(p)) continue;
                if (p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase)) return true;
                var name = p.TrimEnd('/').Substring(p.TrimEnd('/').LastIndexOf('/') + 1);
                if (!name.Contains(".") || AssetDatabase.IsValidFolder(p)) return true;
            }
            return false;
        }
    }
}
