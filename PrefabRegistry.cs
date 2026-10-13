using System.Collections.Generic;

using UnityEngine;

namespace PrefabManager
{
    internal static class PrefabRegistry
    {
        static readonly Dictionary<string, GameObject> _cache = new(System.StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => _cache.Clear();

        internal static bool IsRegistered(string guid)
            => !string.IsNullOrEmpty(guid) && PrefabManifest.Paths.ContainsKey(guid);

        internal static GameObject Load(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            if (TryLoad(guid, out var prefab))
                return prefab;

            if (PrefabManifest.WarningsEnabled)
            {
                if (!IsRegistered(guid))
                    Debug.LogWarning($"[Prefabs] Unknown guid '{guid}'");
                else if (PrefabManifest.TryGetPath(guid, out var path))
                    Debug.LogWarning($"[Prefabs] Failed to load prefab at '{path}' (guid '{guid}')");
            }

            return null;
        }

        internal static bool TryLoad(string guid, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrEmpty(guid)) return false;

            if (_cache.TryGetValue(guid, out prefab))
                return true;

            if (!PrefabManifest.Paths.TryGetValue(guid, out var path))
                return false;

            prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
                return false;

            _cache[guid] = prefab;
            return true;
        }
        internal static TComponent Load<TComponent>(string guid)
            where TComponent : Component
        {
            var cached = Load(guid);
            return cached != null ? cached.GetComponent<TComponent>() : default;
        }
        internal static bool TryLoad<TComponent>(string guid, out TComponent component)
            where TComponent : Component
        {
            component = default;
            if (!TryLoad(guid, out var go) || go == null)
                return false;

            component = go.GetComponent<TComponent>();
            return component != null;
        }

        internal static GameObject Instantiate(string guid, Transform parent = null)
        {
            var prefab = Load(guid);
            return prefab != null ? Object.Instantiate(prefab, parent) : null;
        }

        internal static GameObject Instantiate(string guid, Transform parent, bool worldPositionStays)
        {
            var prefab = Load(guid);
            return prefab != null ? Object.Instantiate(prefab, parent, worldPositionStays) : null;
        }

        internal static GameObject Instantiate(string guid, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var prefab = Load(guid);
            if (prefab == null) return null;
            return parent != null
                ? Object.Instantiate(prefab, position, rotation, parent)
                : Object.Instantiate(prefab, position, rotation);
        }

        internal static TComponent Instantiate<TComponent>(string guid, Vector3 position, Quaternion rotation, Transform parent = null)
            where TComponent : Component
        {
            var go = Instantiate(guid, position, rotation, parent);
            return go != null ? go.GetComponent<TComponent>() : null;
        }

        internal static TComponent Instantiate<TComponent>(string guid, Transform parent)
            where TComponent : Component
        {
            var go = Instantiate(guid, parent);
            return go != null ? go.GetComponent<TComponent>() : null;
        }

        internal static TComponent Instantiate<TComponent>(string guid, Transform parent, bool worldPositionStays)
            where TComponent : Component
        {
            var go = Instantiate(guid, parent, worldPositionStays);
            return go != null ? go.GetComponent<TComponent>() : null;
        }
    }
}