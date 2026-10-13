using PrefabManager.Entities;

using UnityEngine;

namespace PrefabManager
{
    public static class PrefabRefExtensions
    {
        public static bool IsRegistered(this PrefabRef prefab)
            => PrefabManifest.IsRegistered(prefab.Guid);

        /// <summary>Prefab asset without instantiate (cached Resources.Load). Null + warning on failure.</summary>
        public static GameObject Load(this PrefabRef prefab)
            => PrefabRegistry.Load(prefab.Guid);
        /// <summary>
        /// Component on the prefab <b>asset</b>. Do not mutate the result — it is shared.
        /// Null + warning on failure.
        /// </summary>
        public static TComponent Load<TComponent>(this PrefabRef prefab)
            where TComponent : Component
            => PrefabRegistry.Load<TComponent>(prefab.Guid);
        /// <summary>Same as Load, but returns false instead of logging a warning on failure.</summary>
        public static bool TryLoad(this PrefabRef prefab, out GameObject asset)
            => PrefabRegistry.TryLoad(prefab.Guid, out asset);
        public static bool TryLoad<TComponent>(this PrefabRef prefab, out TComponent component)
            where TComponent : Component
            => PrefabRegistry.TryLoad(prefab.Guid, out component);

        public static GameObject Instantiate(this PrefabRef prefab, Transform parent = null)
            => PrefabRegistry.Instantiate(prefab.Guid, parent);
        public static GameObject Instantiate(this PrefabRef prefab, Transform parent, bool worldPositionStays)
            => PrefabRegistry.Instantiate(prefab.Guid, parent, worldPositionStays);
        public static GameObject Instantiate(this PrefabRef prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            => PrefabRegistry.Instantiate(prefab.Guid, position, rotation, parent);
        public static TComponent Instantiate<TComponent>(this PrefabRef prefab, Transform parent)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(prefab.Guid, parent);
        public static TComponent Instantiate<TComponent>(this PrefabRef prefab, Transform parent, bool worldPositionStays)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(prefab.Guid, parent, worldPositionStays);
        public static TComponent Instantiate<TComponent>(this PrefabRef prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(prefab.Guid, position, rotation, parent);
    }
}
