using UnityEditor;

namespace PrefabManager.Editor
{
    [CustomEditor(typeof(PrefabManager.PrefabOverrideMap))]
    public sealed class PrefabOverrideMapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var path = AssetDatabase.GetAssetPath(target);
            if (!IsAutoApplied(path))
                EditorGUILayout.HelpBox(
                    "Not under a Resources folder — " +
                    "it won't auto-apply at startup. Move it under Resources or call Apply() manually.",
                    MessageType.Warning);
        }

        static bool IsAutoApplied(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            var p = path.Replace('\\', '/');
            return p.Contains("/Resources/") ||
                   p.StartsWith("Assets/Resources/");
        }
    }
}
