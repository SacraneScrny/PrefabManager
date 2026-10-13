using System;

using UnityEngine;

namespace PrefabManager.Entities
{
    [Serializable]
    public struct PrefabRef : IEquatable<PrefabRef>
    {
        [SerializeField] string _guid;

        public string Guid    => _guid;
        public bool   IsValid => !string.IsNullOrEmpty(_guid);

        public PrefabRef(string guid) => _guid = guid;

        public static readonly PrefabRef None = default;

        public bool Equals(PrefabRef other)     => string.Equals(_guid, other._guid, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PrefabRef r && Equals(r);
        public override int  GetHashCode()      => _guid != null ? StringComparer.Ordinal.GetHashCode(_guid) : 0;
        public static bool operator ==(PrefabRef a, PrefabRef b) => string.Equals(a._guid, b._guid, StringComparison.Ordinal);
        public static bool operator !=(PrefabRef a, PrefabRef b) => !string.Equals(a._guid, b._guid, StringComparison.Ordinal);
        public override string ToString()       => IsValid ? $"PrefabRef({_guid})" : "PrefabRef(None)";
    }
}