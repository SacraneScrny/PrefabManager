using UnityEngine;

namespace PrefabManager.Entities
{
    public interface IPrefabRef
    {
        PrefabRef GetRef();

        bool IsValid => GetRef().IsValid;
        bool IsRegistered()
            => PrefabManifest.IsRegistered(GetRef().Guid);

        GameObject Load()
            => PrefabRegistry.Load(GetRef().Guid);
        TComponent Load<TComponent>()
            where TComponent : Component
            => PrefabRegistry.Load<TComponent>(GetRef().Guid);

        bool TryLoad(out GameObject asset)
            => PrefabRegistry.TryLoad(GetRef().Guid, out asset);
        bool TryLoad<TComponent>(out TComponent component)
            where TComponent : Component
            => PrefabRegistry.TryLoad(GetRef().Guid, out component);

        GameObject Instantiate(Transform parent = null)
            => PrefabRegistry.Instantiate(GetRef().Guid, parent);
        GameObject Instantiate(Transform parent, bool worldPositionStays)
            => PrefabRegistry.Instantiate(GetRef().Guid, parent, worldPositionStays);
        GameObject Instantiate(Vector3 position, Quaternion rotation, Transform parent = null)
            => PrefabRegistry.Instantiate(GetRef().Guid, position, rotation, parent);
        TComponent Instantiate<TComponent>(Transform parent)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(GetRef().Guid, parent);
        TComponent Instantiate<TComponent>(Transform parent, bool worldPositionStays)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(GetRef().Guid, parent, worldPositionStays);
        TComponent Instantiate<TComponent>(Vector3 position, Quaternion rotation, Transform parent = null)
            where TComponent : Component
            => PrefabRegistry.Instantiate<TComponent>(GetRef().Guid, position, rotation, parent);
    }
}