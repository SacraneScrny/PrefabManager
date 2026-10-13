using System;

using PrefabManager.Entities;

using UnityEditor;

using UnityEngine;

namespace PrefabManager.Editor
{
    /// <summary>PrefabRef as an object field (drag-drop assigns the guid). Runtime struct untouched.</summary>
    [CustomPropertyDrawer(typeof(PrefabRef))]
    public sealed class PrefabRefDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var guidProp = property.FindPropertyRelative("_guid");
            if (guidProp == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ObjectField(line, label, LoadPrefab(guidProp.stringValue), typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
                guidProp.stringValue = picked == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(picked));

            var warning = GetWarning(guidProp.stringValue);
            if (warning != null)
            {
                var warnRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                    position.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(warnRect, warning, MessageType.Warning);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var guidProp = property?.FindPropertyRelative("_guid");
            bool warn = guidProp != null && GetWarning(guidProp.stringValue) != null;
            return warn
                ? EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing
                : EditorGUIUtility.singleLineHeight;
        }

        static GameObject LoadPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static string GetWarning(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return $"Prefab for guid '{guid}' not found.";
            if (!IsInResources(path)) return "Prefab is not inside a Resources folder — it won't resolve at runtime.";
            return null;
        }

        static bool IsInResources(string path)
            => path.StartsWith("Assets/Resources/", StringComparison.Ordinal) ||
               path.Contains("/Resources/", StringComparison.Ordinal);
    }
}
