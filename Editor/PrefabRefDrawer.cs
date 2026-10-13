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
                SetGuidFromObject(guidProp, picked);

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

        static internal GameObject LoadPrefab(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static internal string GetWarning(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return $"Prefab for guid '{guid}' not found.";
            if (!IsInResources(path)) return "Prefab is not inside a Resources folder — it won't resolve at runtime.";
            return null;
        }

        static internal void SetGuidFromObject(SerializedProperty guidProp, UnityEngine.Object picked)
            => guidProp.stringValue = picked == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(picked));

        static bool IsInResources(string path)
            => path.StartsWith("Assets/Resources/", StringComparison.Ordinal) ||
               path.Contains("/Resources/", StringComparison.Ordinal);
    }

    /// <summary>Override-map row as a single line: Original -&gt; Replacement. No foldout.</summary>
    [CustomPropertyDrawer(typeof(PrefabManager.PrefabOverrideMap.Entry))]
    public sealed class PrefabOverrideMapEntryDrawer : PropertyDrawer
    {
        const float ArrowWidth = 28f;
        const float WarnWidth  = 20f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var origProp = property.FindPropertyRelative("Original")?.FindPropertyRelative("_guid");
            var replProp = property.FindPropertyRelative("Replacement")?.FindPropertyRelative("_guid");
            if (origProp == null || replProp == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            float h = EditorGUIUtility.singleLineHeight;

            var origWarn = PrefabRefDrawer.GetWarning(origProp.stringValue);
            var replWarn = PrefabRefDrawer.GetWarning(replProp.stringValue);
            var firstWarn = origWarn ?? replWarn;

            float fieldW = (position.width - ArrowWidth - (firstWarn != null ? WarnWidth : 0)) / 2f;
            var left  = new Rect(position.x, position.y, fieldW, h);
            var arrow = new Rect(left.xMax, position.y, ArrowWidth, h);
            var right = new Rect(arrow.xMax, position.y, fieldW, h);

            EditorGUI.BeginChangeCheck();
            var origObj = EditorGUI.ObjectField(left, PrefabRefDrawer.LoadPrefab(origProp.stringValue), typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
                PrefabRefDrawer.SetGuidFromObject(origProp, origObj);

            EditorGUI.LabelField(arrow, "->", Centered());

            EditorGUI.BeginChangeCheck();
            var replObj = EditorGUI.ObjectField(right, PrefabRefDrawer.LoadPrefab(replProp.stringValue), typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
                PrefabRefDrawer.SetGuidFromObject(replProp, replObj);

            if (firstWarn != null)
                EditorGUI.LabelField(new Rect(right.xMax, position.y, WarnWidth, h), new GUIContent("!", firstWarn));

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        static GUIStyle _centered;
        static GUIStyle Centered()
        {
            if (_centered == null)
                _centered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
            return _centered;
        }
    }
}
